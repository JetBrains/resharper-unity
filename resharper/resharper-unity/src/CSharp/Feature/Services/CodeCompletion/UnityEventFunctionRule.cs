using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
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
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.PsiGen.Util;
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
            if (!CheckPosition(context, out var classDeclaration, out var hasVisibilityModifier, out var hasReturnType))
                return false;

            // Only add items in the light pass. This gives us higher relevance, putting us
            // above types that are in the full pass. Note that we're fast enough to just use
            // Light mode. The worst we do is a little bit of LINQ and checking inheritance.
            // This shouldn't fire as the base class says we only support light evaluation.
            if (context.BasicContext.Parameters.EvaluationMode != EvaluationMode.Light)
                return false;

            // Don't add anything in double completion - we've already added it. This also
            // shouldn't fire, as the base class says we only support single completion.
            if (context.BasicContext.Parameters.Multiplier > 1)
                return true;

            var typeElement = classDeclaration.DeclaredElement;
            if (typeElement == null)
                return false;

            var unityApi = context.BasicContext.Solution.GetComponent<UnityApi>();
            var unityVersionApi = context.BasicContext.Solution.GetComponent<UnityVersion>();
            var project = context.BasicContext.File.GetProject();
            var actualVersion = unityVersionApi.GetActualVersion(project);
            var existingMethods = typeElement.Methods.ToList();
            
            var addedFunctions = new HashSet<string>();
            
            foreach (var function in unityApi.GetEventFunctions(typeElement, actualVersion))
            {
                if (HasAnyPartiallyMatchingExistingMethods(existingMethods, function))
                    continue;

                if (addedFunctions.Contains(function.Name))
                    continue;
                
                // TODO: Decide what to do with e.g. `void OnAnima{caret}`
                // If we want to insert a visibility modifier, it has to go *before* the `void`,
                // which means adding a behaviour here that will remove it
                var addModifier = !hasVisibilityModifier && !hasReturnType;

                var item = CreateMethodItem(context, function, classDeclaration, addModifier);
                if (item == null) continue;

                item = SetRelevanceSortPriority(item, function);
                item = SetLexicographicalSortPriority(item, function);

                addedFunctions.Add(function.Name);
                collector.Add(item);
            }

            return true;
        }

        private bool HasAnyPartiallyMatchingExistingMethods(List<IMethod> existingMethods, UnityEventFunction function)
        {
            // Don't use Any() - it's surprisingly expensive when called for each function we're adding to the lookup list
            foreach (var existingMethod in existingMethods)
            {
                if (function.Match(existingMethod) != MethodSignatureMatch.NoMatch)
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
            UnityEventFunction eventFunction, IClassLikeDeclaration declaration, bool addModifier)
        {
            if (CSharpLanguage.Instance == null)
                return null;

            var method = eventFunction.CreateDeclaration(CSharpElementFactory.GetInstance(declaration, false), declaration);
            if (method.DeclaredElement == null)
                return null;

            var instance = new DeclaredElementInstance(method.DeclaredElement);

            var declaredElementInfo = new DeclaredElementInfoWithoutParameterInfo(method.DeclaredName, instance, CSharpLanguage.Instance,
                context.BasicContext.LookupItemsOwner, context)
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
                    var type = predefinedType.TryGetType(parameter.ClrTypeName);
                    var typeName = type?.GetPresentableName(CSharpLanguage.Instance) ??
                                   parameter.ClrTypeName.ShortName;
                    sb.AppendFormat("{0}{1}{2}", parameter.IsByRef ? "out" : string.Empty,
                        typeName, parameter.IsArray ? "[]" : string.Empty);
                }
                parameters = sb.ToString();
            }
            var text = $"{eventFunction.Name}({parameters})";
            var parameterOffset = eventFunction.Name.Length;
            var modifier = addModifier ? "private " : string.Empty;

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

                        var image = psiIconManager.GetImage(CLRDeclaredElementType.METHOD, PsiIconExtension.Private);
                        var marker = item.Info.Ranges.CreateVisualReplaceRangeMarker();
                        return new SimplePresentation(displayName, image, marker);
                    })
                .WithBehavior(_ => new UnityEventFunctionBehavior(declaredElementInfo, eventFunction))
                .WithMatcher(_ =>
                new ShiftedDeclaredElementMatcher(text, modifier.Length, declaredElementInfo,
                    context.BasicContext.IdentifierMatchingStyle));
        }

        [ContractAnnotation("=> false, classDeclaration: null; => true, classDeclaration: notnull")]
        private bool CheckPosition(CSharpCodeCompletionContext context, out IClassLikeDeclaration classDeclaration,
            out bool hasVisibilityModifier, out bool hasReturnType)
        {
            classDeclaration = null;
            hasVisibilityModifier = false;
            hasReturnType = false;

            // Make sure we're completing an identifier
            if (!(context.UnterminatedContext.TreeNode is ICSharpIdentifier identifier))
                return false;

            // Make sure we're in the correct place for showing Unity event functions.
            if (!ShouldComplete(context.NodeInFile, identifier))
                return false;

            // We know we're in a place where we can complete, so now configure what we
            // complete and what we display
            hasReturnType = HasExistingReturnType(identifier, out var typeUsage);
            hasVisibilityModifier = HasExistingVisibilityModifier(typeUsage);
            classDeclaration = GetClassDeclaration(context.NodeInFile);

            return classDeclaration != null;
        }

        private static bool ShouldComplete(ITreeNode nodeInFile, ICSharpIdentifier identifier)
        {
            var methodDeclaration = identifier.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration != null)
            {
                // Don't complete in the parameter list
                if (nodeInFile.GetContainingNode<IFormalParameterList>() != null)
                    return false;

                // Don't complete in the attribute list
                if (nodeInFile.GetContainingNode<IAttributeSectionList>() != null)
                    return false;

                // Check the whole text of the declaration - if it ends (or even starts)
                // with "__" (which is the completion marker) then we have an incomplete
                // method declaration and we're good to complete at this position
                var declarationText = methodDeclaration.GetText();
                if (declarationText.StartsWith("__") || declarationText.EndsWith("__"))
                    return true;

                // E.g. `OnAni{caret} [SerializeField]` causes the parser to treat
                // the next construct's attribute as an array specifier to the type
                // usage we're typing. (If there's already a type, then the parser
                // thinks it's a property). So, if we have an array rank, check to
                // see if the token following the `[` is an identifier. If so, it's
                // likely it should be an attribute instead, so allow completion.
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
                    return identifier == fieldDeclaration.NameIdentifier;
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

        private static bool HasExistingVisibilityModifier([CanBeNull] ITreeNode typeUsage)
        {
            if (typeUsage == null || !(typeUsage.GetPreviousMeaningfulSibling() is IModifiersList modifiersList))
                return false;

            // TODO: What about virtual or override?
            return (modifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                    modifiersList.HasModifier(CSharpTokenType.INTERNAL_KEYWORD) ||
                    modifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD) ||
                    modifiersList.HasModifier(CSharpTokenType.PRIVATE_KEYWORD));
        }

        [CanBeNull]
        private static IClassLikeDeclaration GetClassDeclaration(ITreeNode completionNode)
        {
            return completionNode.GetContainingNode<IClassLikeDeclaration>();
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