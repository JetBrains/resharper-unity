using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems.Presentation;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Text;
using JetBrains.TextControl;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CodeCompletion
{
    [Language(typeof(CSharpLanguage))]
    public class UnityEventFunctionRule : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
    {
        protected override bool IsAvailable(CSharpCodeCompletionContext context)
        {
            if (!(context.PsiModule is IProjectPsiModule projectPsiModule)
                || !projectPsiModule.Project.IsUnityProject())
            {
                return false;
            }

            var completionTypes = context.BasicContext.Parameters.CodeCompletionTypes;

            // Only allow single completion
            if (completionTypes.Length != 1)
                return false;

            // Don't show for import completion
            if (completionTypes[0] != CodeCompletionType.BasicCompletion
                && completionTypes[0] != CodeCompletionType.SmartCompletion)
            {
                return false;
            }

            return !context.IsQualified;
        }

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            // Assert base class preconditions. We only add items in the light pass, which gives us higher relevance,
            // putting us above types that are in the full pass. We need to be super fast in Light mode, and we are -
            // we're just looking things up
            Assertion.Assert(context.BasicContext.Parameters.EvaluationMode == EvaluationMode.Light, "evaluationMode == EvaluationMode.Light");
            Assertion.Assert(context.BasicContext.Parameters.Multiplier == 1, "multiplier == 1");

            var unityApi = context.BasicContext.Solution.GetComponent<UnityApi>();
            if (!CheckPosition(context, unityApi, out var classDeclaration, out var accessRights, out var hasReturnType))
                return false;

            var typeElement = classDeclaration.DeclaredElement;
            var baseTypeElement = typeElement?.GetBaseClassType()?.GetTypeElement();
            if (typeElement == null || baseTypeElement == null)
                return false;

            var unityVersionApi = context.BasicContext.Solution.GetComponent<UnityVersion>();
            var project = context.BasicContext.File.GetProject();
            var actualVersion = unityVersionApi.GetActualVersion(project);
            var thisMethods = typeElement.Methods.ToList();
            var inheritedMethods = baseTypeElement.GetAllClassMembers<IMethod>().ToList();

            var addedFunctions = new HashSet<string>();

            foreach (var function in unityApi.GetEventFunctions(typeElement, actualVersion))
            {
                if (HasAnyPartiallyMatchingExistingMethods(thisMethods, function))
                    continue;

                if (HasAnyExactMatchInheritedMethods(inheritedMethods, function))
                    continue;

                if (addedFunctions.Contains(function.Name))
                    continue;

                var item = CreateMethodItem(context, function, classDeclaration, hasReturnType, accessRights);
                if (item == null) continue;

                item = SetRelevanceSortPriority(item, function);
                item = SetLexicographicalSortPriority(item, function);

                addedFunctions.Add(function.Name);
                collector.Add(item);
            }

            return true;
        }

        private bool HasAnyPartiallyMatchingExistingMethods(IEnumerable<IMethod> existingMethods,
                                                            UnityEventFunction function)
        {
            // Don't use Any() - it's surprisingly expensive when called for each function we're adding to the lookup list
            foreach (var existingMethod in existingMethods)
            {
                if (function.Match(existingMethod) != MethodSignatureMatch.NoMatch)
                    return true;
            }

            return false;
        }

        private bool HasAnyExactMatchInheritedMethods(IEnumerable<TypeMemberInstance<IMethod>> inheritedMethods,
                                                      UnityEventFunction function)
        {
            foreach (var existingMethod in inheritedMethods)
            {
                if (function.Match(existingMethod.Member) == MethodSignatureMatch.ExactMatch)
                    return true;
            }

            return false;
        }

        private ILookupItem SetRelevanceSortPriority(ILookupItem item, UnityEventFunction function)
        {
            // When items are sorted by relevance, highest value wins. It can be tricky to know if the item is going to
            // be positioned correctly, but essentially, the more flags you have, the higher up in the list you'll be.
            // Language specific flags have the most boost power, so the type of declared element helps a lot (methods
            // are higher than types, but not as high as local variables). Environmental flags will still boost the
            // relevance, but not by as much. So text matching will boost above other items of the same declared element
            // type, but won't boost a method name over a local variable name. The evaluation mode can also give a small
            // but significant boost - Light has higher relevance than Full. Many of these flags are set automatically
            // by looking at the declared element type, or evaluation mode, etc. But we can set some values explicitly,
            // which can give a meaningful boost and get our items to the top of the list
            // Secondary sort order is OrderString, which is usually display text, but can be overridden.

            // Generated items get a boost over normal declared element items
            item.Placement.Relevance |= (long) CLRLookupItemRelevance.GenerateItems;

            // Set high selection priority to push us further up, unless it's undocumented, in which case, give it a
            // smaller selection boost
            item = !function.Undocumented ? item.WithHighSelectionPriority() : item.WithLowSelectionPriority();
            return item;
        }

        // ReSharper disable once UnusedParameter.Local
        private ILookupItem SetLexicographicalSortPriority(ILookupItem item, UnityEventFunction function)
        {
            // When items are sorted lexicographically, first sort key is Location, then Rank, then OrderString.
            // NOTE: Don't use item.PlaceBottom(), because it resets item.Placement, resetting any relevance values
            // Don't do any sorting here. If the user really wants lexicographical sorting, they won't appreciate us
            // messing around with the sorting.
            // I'm willing to be convinced otherwise here. Let's see if anyone shouts (I don't know if anyone even uses
            // lexicographical sorting instead of the default relevance sorting). If we want to change things, push
            // everything to the top, with the undocumented items at the bottom of this group
//            item.Placement.Location = PlacementLocation.Top;
//            item.Placement.Rank = (byte) (function.Undocumented ? 1 : 0);
            return item;
        }

        private static ILookupItem CreateMethodItem(CSharpCodeCompletionContext context,
                                                    UnityEventFunction eventFunction, IClassLikeDeclaration declaration,
                                                    bool hasReturnType, AccessRights accessRights)
        {
            if (CSharpLanguage.Instance == null)
                return null;

            // Only show the modifier in the list text if it's not already specified and there isn't a return type, in
            // which case we default to `private`. E.g. if someone types `OnAnim`, then show `private void OnAnimate...`
            // but if they type `void OnAnim`, they don't want a modifier, and if they type `public void OnAnim` then
            // they want to use `public`
            var showModifier = false;
            if (!hasReturnType && accessRights == AccessRights.NONE)
            {
                showModifier = true;
                accessRights = AccessRights.PRIVATE;
            }

            var factory = CSharpElementFactory.GetInstance(declaration, false);
            var methodDeclaration = eventFunction.CreateDeclaration(factory, declaration, accessRights);
            if (methodDeclaration.DeclaredElement == null)
                return null;

            var instance = new DeclaredElementInstance(methodDeclaration.DeclaredElement);

            var declaredElementInfo = new DeclaredElementInfoWithoutParameterInfo(methodDeclaration.DeclaredName,
                instance, CSharpLanguage.Instance, context.BasicContext.LookupItemsOwner, context)
            {
                Ranges = context.CompletionRanges
            };

            // This is effectively the same as GenerateMemberPresentation, but without the overhead that comes
            // with the flexibility of formatting any time of declared element. We just hard code the format
            var predefinedType = context.PsiModule.GetPredefinedType();
            var parameters = string.Empty;
            if (eventFunction.Parameters.Length > 0)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < eventFunction.Parameters.Length; i++)
                {
                    if (i > 0) sb.Append(", ");

                    var parameter = eventFunction.Parameters[i];
                    var type = predefinedType.TryGetType(parameter.ClrTypeName, NullableAnnotation.Unknown);
                    var typeName = type?.GetPresentableName(CSharpLanguage.Instance) ??
                                   parameter.ClrTypeName.ShortName;
                    sb.AppendFormat("{0}{1}{2}", parameter.IsByRef ? "out" : string.Empty,
                        typeName, parameter.IsArray ? "[]" : string.Empty);
                }
                parameters = sb.ToString();
            }
            var text = $"{eventFunction.Name}({parameters})";
            var parameterOffset = eventFunction.Name.Length;

            var modifier = showModifier
                ? CSharpDeclaredElementPresenter.Instance.Format(accessRights) + " "
                : string.Empty;

            var psiIconManager = context.BasicContext.LookupItemsOwner.Services.PsiIconManager;

            return LookupItemFactory.CreateLookupItem(declaredElementInfo)
                .WithPresentation(item =>
                {
                    var displayName = new RichText($"{modifier}{text} {{ ... }}");

                    // GenerateMemberPresentation marks everything as bold, and the parameters + block syntax as not important
                    var parameterStartOffset = modifier.Length + parameterOffset;
                    LookupUtil.MarkAsNotImportant(displayName,
                        TextRange.FromLength(parameterStartOffset, displayName.Length - parameterStartOffset));
                    LookupUtil.AddEmphasize(displayName, new TextRange(modifier.Length, displayName.Length));

                    var image = psiIconManager.GetImage(methodDeclaration.DeclaredElement,
                        methodDeclaration.DeclaredElement.PresentationLanguage, true);
                    var marker = item.Info.Ranges.CreateVisualReplaceRangeMarker();
                    return new SimplePresentation(displayName, image, marker);
                })
                .WithBehavior(_ => new UnityEventFunctionBehavior(declaredElementInfo, eventFunction, accessRights))
                .WithMatcher(_ =>
                    new ShiftedDeclaredElementMatcher(eventFunction.Name, modifier.Length, declaredElementInfo,
                        context.BasicContext.IdentifierMatchingStyle));
        }

        [ContractAnnotation("=> false, classDeclaration: null; => true, classDeclaration: notnull")]
        private bool CheckPosition(CSharpCodeCompletionContext context, UnityApi unityApi,
                                   out IClassDeclaration classDeclaration,
                                   out AccessRights accessRights, out bool hasReturnType)
        {
            classDeclaration = GetClassDeclaration(context.NodeInFile);
            accessRights = AccessRights.NONE;
            hasReturnType = false;

            if (classDeclaration == null)
                return false;

            // Make sure we're completing an identifier
            if (!(context.UnterminatedContext.TreeNode is ICSharpIdentifier identifier))
                return false;

            if (!unityApi.IsUnityType(classDeclaration.DeclaredElement))
                return false;

            // Make sure we're in the correct place for showing Unity event functions.
            if (!ShouldComplete(context.NodeInFile, identifier))
                return false;

            // We know we're in a place where we can complete, so now configure what we
            // complete and what we display
            hasReturnType = HasExistingReturnType(identifier, out var typeUsage);
            accessRights = GetAccessRights(typeUsage);

            return true;
        }

        private static bool ShouldComplete(ITreeNode nodeInFile, ICSharpIdentifier identifier)
        {
            var methodDeclaration = identifier.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration != null)
            {
                // Don't complete in the parameter list
                if (nodeInFile.GetContainingNode<IFormalParameterList>() != null) return false;

                // Don't complete in the attribute list
                if (nodeInFile.GetContainingNode<IAttributeSectionList>() != null) return false;

                // Don't complete if there is a preceding [SerializeField] attribute
                if (HasSerializedFieldAttribute(methodDeclaration)) return false;

                // Check the whole text of the declaration - if it ends (or even starts) with "__" (which is the
                // completion marker) then we have an incomplete method declaration and we're good to complete at this
                // position
                var declarationText = methodDeclaration.GetText();
                if (declarationText.StartsWith("__") || declarationText.EndsWith("__"))
                    return true;

                if (identifier == methodDeclaration.NameIdentifier)
                    return true;

                // E.g. `OnAni{caret} [SerializeField]` causes the parser to treat the next construct's attribute as an
                // array specifier to the type usage we're typing. (If there's already a type, then the parser thinks
                // it's a property). So, if we have an array rank, check to see if the token following the `[` is an
                // identifier. If so, it's likely it should be an attribute instead, so allow completion.
                var typeUsage = identifier.GetContainingNode<ITypeUsage>();
                if (typeUsage != null)
                {
                    var arrayRanks = typeUsage.ArrayRanks;
                    if (arrayRanks.Count > 0)
                    {
                        var lbracket = arrayRanks[0].LBracket;
                        var next = lbracket.GetNextMeaningfulSibling();
                        if (next == null)
                        {
                            next = lbracket.GetNextMeaningfulToken();
                            if (next != null && next.NodeType == CSharpTokenType.IDENTIFIER)
                                return true;
                        }
                    }
                }

                return false;
            }

            // E.g. `public void OnAni{caret} [SerializeField]` causes the parser to
            // treat this as a field declaration, using the brackets as an array specifier
            var fieldDeclaration = identifier.GetContainingNode<IFieldDeclaration>();
            if (fieldDeclaration?.LBracket != null)
            {
                if (fieldDeclaration.FixedBufferSizeExpression == null
                    || !fieldDeclaration.FixedBufferSizeExpression.IsConstantValue())
                {
                    if (HasSerializedFieldAttribute(fieldDeclaration))
                        return false;

                    return identifier == fieldDeclaration.NameIdentifier;
                }
            }

            return false;
        }

        private static bool HasSerializedFieldAttribute(ICSharpTypeMemberDeclaration declaration)
        {
            foreach (var attribute in declaration.AttributesEnumerable)
            {
                var result = attribute.TypeReference?.Resolve();
                if (result != null && result.ResolveErrorType.IsAcceptable)
                {
                    if (result.DeclaredElement is IClass declaredElement &&
                        Equals(declaredElement.GetClrName(), KnownTypes.SerializeField))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasExistingReturnType(ITreeNode identifier, [CanBeNull] out ITypeUsage typeUsage)
        {
            // Only return true if the user has explicitly typed a return type.
            // If they haven't, then our completion identifier will be parsed
            // as a type usage. Return the usage any way, we need it elsewhere.
            typeUsage = identifier.GetContainingNode<ITypeUsage>();
            if (typeUsage == null)
            {
                typeUsage = identifier.GetPreviousMeaningfulSibling() as ITypeUsage;
                if (typeUsage == null)
                {
                    var fieldDeclaration = identifier.GetContainingNode<IFieldDeclaration>();
                    typeUsage = fieldDeclaration?.GetPreviousMeaningfulSibling() as ITypeUsage;
                }
                return typeUsage != null;
            }
            return false;
        }

        private static AccessRights GetAccessRights([CanBeNull] ITreeNode typeUsage)
        {
            if (typeUsage == null || !(typeUsage.GetPreviousMeaningfulSibling() is IModifiersList modifiersList))
                return AccessRights.NONE;

            return ModifiersUtil.GetAccessRightsModifiers(modifiersList);
        }

        [CanBeNull]
        private static IClassDeclaration GetClassDeclaration(ITreeNode completionNode)
        {
            return completionNode.GetContainingNode<IClassDeclaration>();
        }

        // The code completion tooltip tries to show parameter info, if available. We already show this as part of the
        // lookup item text, so it's pointless showing it again. If there aren't any parameter info candidates, the
        // tooltip will show a custom description via IDescriptionProvidingLookupItem, which LookupItem<T> doesn't
        // implement. Fortunately, it also falls back to showing the standard declared element description, so we'll
        // get a proper description after all.
        private class DeclaredElementInfoWithoutParameterInfo : DeclaredElementInfo
        {
            public DeclaredElementInfoWithoutParameterInfo(string methodDeclaredName, DeclaredElementInstance instance,
                                                           PsiLanguageType language,
                                                           ILookupItemsOwner lookupItemsOwner,
                                                           IElementPointerFactory elementPointerFactory)
                : base(methodDeclaredName, instance, language, lookupItemsOwner, elementPointerFactory)
            {
            }

            public override bool HasCandidates => false;
            public override IEnumerable<InvocationCandidate> Candidates => EmptyList<InvocationCandidate>.Enumerable;
        }

        // DeclaredElementMatcher can take a custom text to match against, but ReSharper applies the matching result to
        // the display text, so it looks wrong. Interestingly, Rider gets it right. Don't know why they're difference.
        // This class will shift the match result by a given value. It assumes that the custom text is the tail of the
        // display text and makes no other modifications to the matched offsets
        private class ShiftedDeclaredElementMatcher : DeclaredElementMatcher
        {
            private readonly int myShiftOffset;

            public ShiftedDeclaredElementMatcher(string customText, int shiftOffset,
                                                 DeclaredElementInfo declaredElementInfo,
                                                 IdentifierMatchingStyle matchingStyle)
                : base(customText, declaredElementInfo, matchingStyle)
            {
                myShiftOffset = shiftOffset;
            }

            public override MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
            {
                var result = base.Match(prefixMatcher, textControl);
                return result?.Shift(myShiftOffset);
            }
        }
    }
}