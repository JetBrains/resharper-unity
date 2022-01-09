using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Impl;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.LookupItems.Presentation;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
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
            var generationContext = TryBuildMemberGenerationContext(context);
            if (generationContext == null) return false;

            // Assert base class preconditions. We only add items in the light pass, which gives us higher relevance,
            // putting us above types that are in the full pass. We need to be super fast in Light mode, and we are -
            // we're just looking things up
            Assertion.Assert(context.BasicContext.Parameters.EvaluationMode == EvaluationMode.Light,
                "evaluationMode == EvaluationMode.Light");
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
            var shouldFunctionGenerateMethod = ShouldGenerateMethod(context);
            var knownTypesCache = context.BasicContext.Solution.GetComponent<KnownTypesCache>();

            var addedFunctions = new HashSet<string>();

            foreach (var function in unityApi.GetEventFunctions(typeElement, actualVersion))
            {
                if (HasAnyPartiallyMatchingExistingMethods(thisMethods, function))
                    continue;

                if (HasAnyExactMatchInheritedMethods(inheritedMethods, function))
                    continue;

                if (addedFunctions.Contains(function.Name))
                    continue;

                if (HasDifferentParameterLists(function, context, knownTypesCache))
                    continue;

                var item = CreateMethodItem(context, function, classDeclaration, hasReturnType, shouldFunctionGenerateMethod, accessRights, generationContext);
                if (item == null) continue;

                item = SetRelevanceSortPriority(item, function);
                item = SetLexicographicalSortPriority(item, function);

                addedFunctions.Add(function.Name);
                collector.Add(item);
            }

            return true;
        }

        private static bool HasDifferentParameterLists(
            [NotNull] UnityEventFunction function,
            [NotNull] CSharpCodeCompletionContext context,
            [NotNull] KnownTypesCache knownTypesCache)
        {
            var unterminatedContext = context.UnterminatedContext;
            var methodDeclaration = unterminatedContext.TreeNode?.GetContainingNode<IMethodDeclaration>();

            if (methodDeclaration == null) return false;

            var functionParameters = function.Parameters;
            var currentMethodParameters = methodDeclaration.Params;
            if (currentMethodParameters == null) return false;

            if (functionParameters.Length != currentMethodParameters.ParameterDeclarations.Count) return true;

            var typeConversionRule = currentMethodParameters.GetTypeConversionRule();
            var module = methodDeclaration.GetPsiModule();
            for (var paramIndex = 0; paramIndex < functionParameters.Length; paramIndex++)
            {
                var functionParameter = functionParameters[paramIndex];
                var currentMethodParameter = currentMethodParameters.ParameterDeclarations[paramIndex];

                var conversion = typeConversionRule.ClassifyImplicitConversion(currentMethodParameter.Type, functionParameter.TypeSpec.AsIType(knownTypesCache, module));
                if (conversion.Kind != ConversionKind.Identity) return true;
            }

            return false;
        }

        private static bool ShouldGenerateMethod([NotNull] CSharpCodeCompletionContext context)
        {
            var unterminatedContext = context.UnterminatedContext;
            var methodDeclaration = unterminatedContext.TreeNode?.GetContainingNode<IMethodDeclaration>();

            if (methodDeclaration?.Body == null)
            {
                // If we're renaming the entire method name, we end up with two broken method declarations, one with a
                // missing method body, immediately followed by another with a missing name, but the first meaningful
                // child - the parameter list from the original method, and the return value in the second - is a tuple
                // type usage
                foreach (var treeNode in context.NodeInFile.ContainingNodes(true))
                {
                    if (treeNode.IsFiltered())
                    {
                        var prevMeaningfulSibling = treeNode.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments();
                        var nextMeaningfulSibling = treeNode.GetNextMeaningfulSibling();

                        if (prevMeaningfulSibling is IMethodDeclaration prevMethod
                            && nextMeaningfulSibling is IMethodDeclaration nextMethod
                            && prevMethod.Body == null
                            && nextMethod.Body != null
                            && nextMethod.GetNextMeaningfulChild(null) is ITupleTypeUsage)
                        {
                            return !(prevMethod.GetContainingTypeDeclaration() is IClassLikeDeclaration);
                        }
                    }
                }

                return true;
            }

            if (!(methodDeclaration.GetContainingTypeDeclaration() is IClassLikeDeclaration))
                return true;

            return false;
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
            item.Placement.Relevance |= (long) (CLRLookupItemRelevance.GenerateItems | CLRLookupItemRelevance.Methods |
                                                CLRLookupItemRelevance.MemberOfCurrentType);

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

        private ILookupItem CreateMethodItem(CSharpCodeCompletionContext context,
                                             UnityEventFunction eventFunction, IClassLikeDeclaration declaration,
                                             bool hasReturnType, bool shouldGenerateMethod,
                                             AccessRights accessRights, MemberGenerationContext generationContext)
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

            // Note that we can't keep this declaration - it will become invalid as the user types to narrow down the
            // search and modifies the PSI file. This only affects ReSharper, Rider has different code completion
            // mechanism
            var factory = CSharpElementFactory.GetInstance(declaration, false);
            var knownTypesCache = context.BasicContext.Solution.GetComponent<KnownTypesCache>();
            var methodDeclaration = eventFunction.CreateDeclaration(factory, knownTypesCache, declaration, accessRights);
            if (methodDeclaration.DeclaredElement == null)
                return null;

            // This is effectively the same as GenerateMemberPresentation, but without the overhead that comes
            // with the flexibility of formatting any time of declared element. We just hard code the format
            var parameters = string.Empty;
            if (eventFunction.Parameters.Length > 0)
            {
                var sb = new StringBuilder();
                for (var i = 0; i < eventFunction.Parameters.Length; i++)
                {
                    if (i > 0) sb.Append(", ");

                    var parameter = eventFunction.Parameters[i];
                    var type = parameter.TypeSpec.AsIType(knownTypesCache, context.PsiModule);
                    var typeName = type.GetPresentableName(CSharpLanguage.Instance);
                    sb.AppendFormat("{0}{1}", parameter.IsByRef ? "out" : string.Empty, typeName);
                }
                parameters = sb.ToString();
            }
            var text = $"{eventFunction.Name}({parameters})";
            var parameterOffset = eventFunction.Name.Length;

            var modifier = showModifier
                ? CSharpDeclaredElementPresenter.Instance.Format(accessRights) + " "
                : string.Empty;

            var psiIconManager = context.BasicContext.LookupItemsOwner.Services.PsiIconManager;

            // Note that because this is a text based lookup item, then it won't be included if the normal C# method
            // filter is applied. We can't make it a method based lookup item because the DeclaredElement isn't valid in
            // this situation - it's not a real method, and a DeclaredElementInfo would try to store a pointer to it,
            // and be unable to recreate it when it's needed.
            var textualInfo = new UnityEventFunctionTextualInfo(generationContext.MemberReplaceRanges, shouldGenerateMethod, text, text) {Ranges = context.CompletionRanges};

            var lookupItem = LookupItemFactory.CreateLookupItem(textualInfo)
                .WithPresentation(item =>
                {
                    var displayName = new RichText($"{modifier}{text} {{ ... }}");

                    // GenerateMemberPresentation marks everything as bold, and the parameters + block syntax as not important
                    var parameterStartOffset = modifier.Length + parameterOffset;
                    LookupUtil.MarkAsNotImportant(displayName,
                        TextRange.FromLength(parameterStartOffset, displayName.Length - parameterStartOffset));
                    LookupUtil.AddEmphasize(displayName, new TextRange(modifier.Length, displayName.Length));

                    var image = psiIconManager.GetImage(CLRDeclaredElementType.METHOD);
                    psiIconManager.AttachExtensions(image, GetAccessExtensions(accessRights));
                    var marker = item.Info.Ranges.CreateVisualReplaceRangeMarker();
                    return new SimplePresentation(displayName, image, marker);
                })
                .WithBehavior(_ => new UnityEventFunctionBehavior(textualInfo, eventFunction, accessRights))
                .WithMatcher(_ =>
                    new ShiftedDeclaredElementMatcher(eventFunction.Name, modifier.Length, textualInfo));

            var description = GetDescription(context, methodDeclaration);
            return new WrappedLookupItem<UnityEventFunctionTextualInfo>(lookupItem, description);
        }

        private RichTextBlock GetDescription(CSharpCodeCompletionContext context, IMethodDeclaration methodDeclaration)
        {
            if (methodDeclaration.DeclaredElement == null)
                return null;

            var presenter = context.BasicContext.Solution.GetComponent<IDeclaredElementDescriptionPresenter>();
            var richTextBlock = new RichTextBlock(new RichTextBlockParameters(1));
            var psiLanguageType = methodDeclaration.Language;
            var declaredElementInstance = new DeclaredElementInstance(methodDeclaration.DeclaredElement);
            richTextBlock.Add(DeclaredElementPresenter.Format(psiLanguageType, ourPresenter, declaredElementInstance)
                .Capitalize());
            var description = presenter.GetDeclaredElementDescription(methodDeclaration.DeclaredElement,
                DeclaredElementDescriptionStyle.SUMMARY_STYLE, psiLanguageType);
            if (!RichTextBlock.IsNullOrEmpty(description)) richTextBlock.AddLines(description);

            return richTextBlock;
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
                if (declarationText.StartsWith("__") || declarationText.EndsWith("__") ||
                    declarationText.Contains(SyntheticComments.CodeCompletionIdentifierToken))
                {
                    return true;
                }

                if (identifier == methodDeclaration.NameIdentifier)
                    return true;

                // E.g. `OnAni{caret} [SerializeField]` causes the parser to treat the next construct's attribute as an
                // array specifier to the type usage we're typing. (If there's already a type, then the parser thinks
                // it's a property). So, if we have an array rank, check to see if the token following the `[` is an
                // identifier. If so, it's likely it should be an attribute instead, so allow completion.
                var typeUsage = identifier.GetContainingNode<IArrayTypeUsage>();
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

        private static PsiIconExtension GetAccessExtensions(AccessRights access)
        {
            switch (access)
            {
                case AccessRights.PUBLIC:
                    return PsiIconExtension.Public;
                case AccessRights.INTERNAL:
                    return PsiIconExtension.Internal;
                case AccessRights.PRIVATE:
                    return PsiIconExtension.Private;
                case AccessRights.PROTECTED:
                case AccessRights.PROTECTED_AND_INTERNAL:
                    return PsiIconExtension.Protected;
                case AccessRights.PROTECTED_OR_INTERNAL:
                    return PsiIconExtension.ProtectedInternal;
                case AccessRights.NONE:
                    return PsiIconExtension.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(access), access, null);
            }
        }

        // DeclaredElementMatcher can take a custom text to match against, but ReSharper applies the matching result to
        // the display text, so it looks wrong. Interestingly, Rider gets it right. Don't know why they're different.
        // This class will shift the match result by a given value. It assumes that the custom text is the tail of the
        // display text and makes no other modifications to the matched offsets
        private class ShiftedDeclaredElementMatcher : TextualMatcher<TextualInfo>
        {
            private readonly int myShiftOffset;

            public ShiftedDeclaredElementMatcher(string customText, int shiftOffset, TextualInfo textualInfo)
                : base(customText, textualInfo)
            {
                myShiftOffset = shiftOffset;
            }

            public override MatchingResult Match(PrefixMatcher prefixMatcher)
            {
                var result = base.Match(prefixMatcher);
                return result?.Shift(myShiftOffset);
            }
        }

        private static readonly DeclaredElementPresenterStyle ourPresenter = new DeclaredElementPresenterStyle
        {
            ShowName = NameStyle.QUALIFIED,
            ShowEntityKind = EntityKindForm.NORMAL,
            ShowType = TypeStyle.DEFAULT,
            ShowTypesQualified = false,
            TextStyles = DeclaredElementPresenterTextStyles.ParameterInfo
        };

        private class WrappedLookupItem<TInfo> : IWrappedLookupItem, IDescriptionProvidingLookupItem, IAspectLookupItem<TInfo>
            where TInfo : class, ILookupItemInfo
        {
            private readonly LookupItem<TInfo> myLookupItem;
            [CanBeNull] private readonly RichTextBlock myDescription;

            public WrappedLookupItem(LookupItem<TInfo> lookupItem, [CanBeNull] RichTextBlock description)
            {
                myLookupItem = lookupItem;
                myDescription = description;
            }

            public ILookupItem Item => myLookupItem;

            public RichTextBlock GetDescription() => myDescription;

            #region Delegation

            public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
            {
                return myLookupItem.AcceptIfOnlyMatched(itemAcceptanceContext);
            }

            public MatchingResult Match(PrefixMatcher prefixMatcher)
            {
                return myLookupItem.Match(prefixMatcher);
            }

            public void Accept(ITextControl textControl, DocumentRange nameRange, LookupItemInsertType insertType,
                               Suffix suffix, ISolution solution, bool keepCaretStill)
            {
                myLookupItem.Accept(textControl, nameRange, insertType, suffix, solution, keepCaretStill);
            }

            public DocumentRange GetVisualReplaceRange(DocumentRange nameRange)
            {
                return myLookupItem.GetVisualReplaceRange(nameRange);
            }

            public bool Shrink()
            {
                return myLookupItem.Shrink();
            }

            public void Unshrink()
            {
                myLookupItem.Unshrink();
            }

            public LookupItemPlacement Placement => myLookupItem.Placement;

            public IconId Image => myLookupItem.Image;

            public RichText DisplayName => myLookupItem.DisplayName;

            public RichText DisplayTypeName => myLookupItem.DisplayTypeName;

            public bool CanShrink => myLookupItem.CanShrink;

            public bool IsDynamic => myLookupItem.IsDynamic;

            public bool IgnoreSoftOnSpace => myLookupItem.IgnoreSoftOnSpace;

            public int Identity => myLookupItem.Identity;

            public ILookupItemBehavior Behavior => myLookupItem.Behavior;

            public ILookupItemPresentation Presentation => myLookupItem.Presentation;

            public ILookupItemMatcher Matcher => myLookupItem.Matcher;

            public void UpdateBehavior<TBehavior>(Func<TBehavior, TBehavior> fUpdateBehavior) where TBehavior : class, ILookupItemBehavior
                => myLookupItem.UpdateBehavior(fUpdateBehavior);

            public void UpdatePresentation<TPresentation>(Func<TPresentation, TPresentation> fUpdatePresentation) where TPresentation : class, ILookupItemPresentation
                => myLookupItem.UpdatePresentation(fUpdatePresentation);

            public void UpdatePresentationOfType<TPresentation>(Func<TPresentation, TPresentation> fUpdatePresentation) where TPresentation : class, ILookupItemPresentation
                => myLookupItem.UpdatePresentationOfType(fUpdatePresentation);

            public TInfo Info => myLookupItem.Info;

            #endregion
        }

        // TODO [vkrasnotsvetov, 193] : patch this logic in JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.Rules.Generate.OverrideRule
        // and remove copy-paste from product.
        [CanBeNull]
        private static MemberGenerationContext TryBuildMemberGenerationContext(
            [NotNull] CSharpCodeCompletionContext context)
        {
            var identifier = context.TerminatedContext.TreeNode as ICSharpIdentifier;
            if (identifier == null) return null;

            // override int __
            var fieldDeclaration = FieldDeclarationNavigator.GetByNameIdentifier(identifier);
            if (fieldDeclaration != null)
            {
                var fieldTypeUsage =
                    fieldDeclaration.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments() as ITypeUsage;
                if (fieldTypeUsage == null) return null;

                if (!CheckNodesBeforeTypeUsage(fieldTypeUsage, out var modifiersList)) return null;

                var memberReplaceRanges = ComputeMemberReplaceRanges(TreeOffset.InvalidOffset);
                if (memberReplaceRanges == null) return null;

                var typeDeclaration = GetPhysicalNonStaticTypeDeclaration();
                if (typeDeclaration == null) return null;

                if (modifiersList.ContainsPreprocessorDirectiveChildren()) return null;

                return new MemberGenerationContext(typeDeclaration, modifiersList, fieldTypeUsage, memberReplaceRanges);
            }

            // override __;
            var referenceName = ReferenceNameNavigator.GetByNameIdentifier(identifier);
            var userTypeUsage = UserTypeUsageNavigator.GetByScalarTypeName(referenceName);
            var methodDeclaration = MethodDeclarationNavigator.GetByTypeUsage(userTypeUsage);
            if (methodDeclaration != null)
            {
                if (!CheckNodesBeforeTypeUsage(userTypeUsage, out var modifiersList)) return null;

                var methodStartRange =
                    context.TerminatedContext.ToOriginalTreeRange(
                        new TreeTextRange(methodDeclaration.GetTreeStartOffset()));

                var memberReplaceRanges = ComputeMemberReplaceRanges(methodStartRange.StartOffset);
                if (memberReplaceRanges == null) return null;

                var typeDeclaration = GetPhysicalNonStaticTypeDeclaration();
                if (typeDeclaration == null) return null;

                if (modifiersList.ContainsPreprocessorDirectiveChildren()) return null;

                return new MemberGenerationContext(typeDeclaration, modifiersList, expectedReturnTypeUsage: null,
                    memberReplaceRanges);
            }

            return null;

            bool CheckNodesBeforeTypeUsage(ITypeUsage typeUsage, out IModifiersList modifiersList)
            {
                var previousSibling = typeUsage.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments();
                modifiersList = previousSibling as IModifiersList;

                if (previousSibling is IModifiersList)
                    previousSibling = previousSibling.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments();

                if (previousSibling is IAttributeSectionList)
                    previousSibling = previousSibling.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments();

                if (previousSibling is IDocCommentBlock)
                    previousSibling = previousSibling.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments();

                return previousSibling == null;
            }

            bool IsDeclarationToReplace(ITreeNode declaration)
            {
                switch (declaration)
                {
                    case IMultipleFieldDeclaration _:
                    case IMethodDeclaration _:
                    case IPropertyDeclaration _:
                    case IIndexerDeclaration _:
                        return true;

                    default:
                        return false;
                }
            }

            TextLookupRanges ComputeMemberReplaceRanges(TreeOffset startOffset)
            {
                foreach (var treeNode in context.NodeInFile.ContainingNodes(returnThis: true))
                {
                    if (IsDeclarationToReplace(treeNode))
                    {
                        return RangesForDeclaration(treeNode);
                    }

                    if (treeNode.IsFiltered())
                    {
                        var prevMeaningfulSibling = treeNode.GetPreviousMeaningfulSiblingThroughWhitespaceAndComments();
                        if (prevMeaningfulSibling != null
                            && IsDeclarationToReplace(prevMeaningfulSibling)
                            && (!startOffset.IsValid() || prevMeaningfulSibling.GetTreeStartOffset() >= startOffset))
                        {
                            return RangesForDeclaration(prevMeaningfulSibling);
                        }

                        var nextMeaningfulSibling = treeNode.GetNextMeaningfulSibling();
                        if (nextMeaningfulSibling != null && IsDeclarationToReplace(nextMeaningfulSibling))
                        {
                            return RangesForDeclaration(nextMeaningfulSibling);
                        }
                    }

                    if (treeNode is IFieldDeclaration field)
                    {
                        var range = context.CompletionRanges.ReplaceRange;
                        if (field.GetPreviousMeaningfulSibling() is ITypeUsage typeUsage)
                        {
                            range = range.SetStartTo(typeUsage.GetDocumentStartOffset());
                        }

                        return new TextLookupRanges(range, range);
                    }

                    if (treeNode is ICSharpTypeMemberDeclaration)
                    {
                        return context.CompletionRanges;
                    }
                }

                return null;

                TextLookupRanges RangesForDeclaration(ITreeNode treeNode)
                {
                    var elementRange = ComputeReplaceRangeForDeclaration(treeNode);
                    if (!elementRange.IsValid()) return null;

                    return CodeCompletionContextProviderBase.GetTextLookupRanges(context.BasicContext, elementRange);
                }

                DocumentRange ComputeReplaceRangeForDeclaration(ITreeNode declaration)
                {
                    var startOffsetRange = new DocumentRange(GetDeclarationStartDocumentOffset(declaration));
                    var selectedRange = context.BasicContext.SelectedRange;

                    var anchorDeclaration = GetAnchorDeclaration(declaration);

                    var elementRange = startOffsetRange.Join(selectedRange)
                        .JoinRight(new DocumentRange(anchorDeclaration.GetDocumentEndOffset()));
                    if (!elementRange.IsValid())
                    {
                        elementRange = startOffsetRange.Join(selectedRange);
                    }

                    if (!elementRange.IsValid()) return DocumentRange.InvalidRange;

                    var anchorDeclarationNameRange = GetDeclarationNameRange(anchorDeclaration);
                    if (anchorDeclarationNameRange.IsValid())
                    {
                        var declarationNameLine = anchorDeclarationNameRange.StartOffset.ToDocumentCoords().Line;
                        var selectionLine = selectedRange.EndOffset.ToDocumentCoords().Line;
                        if (declarationNameLine != selectionLine)
                        {
                            return elementRange.SetEndTo(selectedRange.EndOffset);
                        }
                    }

                    return elementRange;
                }

                DocumentOffset GetDeclarationStartDocumentOffset(ITreeNode declaration)
                {
                    // note: do not include attributes into member range
                    var firstChild = declaration.GetNextMeaningfulChild(null);
                    if (firstChild is IDocCommentBlock)
                        firstChild = firstChild.GetNextMeaningfulSibling();

                    if (firstChild is IAttributeSectionList)
                        firstChild = firstChild.GetNextMeaningfulSibling();

                    return (firstChild ?? declaration).GetDocumentStartOffset();
                }

                ITreeNode GetAnchorDeclaration(ITreeNode declaration)
                {
                    if (declaration is IMethodDeclaration unfinishedMethod
                        && unfinishedMethod.NameIdentifier == null
                        && unfinishedMethod.LPar == null
                        && unfinishedMethod.ModifiersList == null
                        && unfinishedMethod.TypeUsage != null)
                    {
                        var nextDeclaration = unfinishedMethod.GetNextMeaningfulSibling();

                        if (IsDeclarationToReplace(nextDeclaration))
                            return nextDeclaration;
                    }

                    return declaration;
                }

                DocumentRange GetDeclarationNameRange(ITreeNode declaration)
                {
                    switch (declaration)
                    {
                        case IMultipleFieldDeclaration fieldDecl:
                            return fieldDecl.DeclaratorsEnumerable.FirstOrDefault().GetDocumentRange();
                        case IDeclaration decl:
                            return decl.GetNameDocumentRange();
                        default:
                            return declaration.GetDocumentRange();
                    }
                }
            }

            ITypeDeclaration GetPhysicalNonStaticTypeDeclaration()
            {
                foreach (var typeDeclaration in context.NodeInFile.ContainingNodes<ITypeDeclaration>())
                {
                    if (typeDeclaration.GetNameRange().EndOffset > context.BasicContext.CaretTreeOffset) continue;

                    switch (typeDeclaration)
                    {
                        case IClassDeclaration classDeclaration when !classDeclaration.IsStatic:
                            return classDeclaration;
                        case IStructDeclaration structDeclaration:
                            return structDeclaration;
                        default:
                            return null;
                    }
                }

                return null;
            }
        }

        private sealed class MemberGenerationContext
        {
            [NotNull] public ITypeDeclaration TypeDeclaration { get; }
            [NotNull] public IPsiModule PsiModule { get; }
            public AccessRights ExpectedAccessRights { get; }
            [NotNull] public TextLookupRanges MemberReplaceRanges { get; }

            [CanBeNull] public IModifiersList ModifiersList { get; }

            [CanBeNull] public IType ExpectedReturnType { get; }

            public bool HasOverrideKeywordTail => ExpectedReturnType == null && HasOverrideTail(ModifiersList);
            public bool HasOverrideKeyword => ModifiersList.HasModifier(CSharpTokenType.OVERRIDE_KEYWORD);
            [NotNull] public PsiLanguageType Language => TypeDeclaration.Language;

            public MemberGenerationContext(
                [NotNull] ITypeDeclaration typeDeclaration,
                [CanBeNull] IModifiersList modifiersList,
                [CanBeNull] ITypeUsage expectedReturnTypeUsage,
                [NotNull] TextLookupRanges memberReplaceRanges)
            {
                TypeDeclaration = typeDeclaration;
                PsiModule = typeDeclaration.GetPsiModule();
                ModifiersList = modifiersList;
                MemberReplaceRanges = memberReplaceRanges;
                ExpectedAccessRights = ModifiersUtil.GetAccessRightsModifiers(modifiersList);

                if (expectedReturnTypeUsage != null)
                {
                    ExpectedReturnType = CSharpTypeFactory.CreateType(expectedReturnTypeUsage);
                }
            }

            [Pure]
            private static bool HasOverrideTail([CanBeNull] IModifiersList modifiersList)
            {
                var modifier = modifiersList?.FindPreviousModifier(anchor: null);
                return modifier != null && modifier.GetTokenType() == CSharpTokenType.OVERRIDE_KEYWORD;
            }

            [Pure]
            public bool MatchesAccessRights([NotNull] IAccessRightsOwner accessRightsOwner)
            {
                if (ExpectedAccessRights == AccessRights.NONE) return true;

                return ExpectedAccessRights == accessRightsOwner.GetAccessRightsVisibleToModule(PsiModule);
            }
        }
    }

    public class UnityEventFunctionTextualInfo : TextualInfo
    {
        public TextLookupRanges MemberReplaceRanges { get; }
        public bool ShouldGenerateMethod { get; }

        public UnityEventFunctionTextualInfo(
            TextLookupRanges memberReplaceRanges,
            bool shouldGenerateMethod,
            [NotNull] string text,
            [NotNull] string identity)
            : base(text, identity)
        {
            MemberReplaceRanges = memberReplaceRanges;
            ShouldGenerateMethod = shouldGenerateMethod;
        }
    }
}
