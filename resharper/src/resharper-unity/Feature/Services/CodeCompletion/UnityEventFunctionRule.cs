using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.CodeCompletion
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
            var functions = unityApi.GetEventFunctions(typeElement, actualVersion);

            var items = new List<ILookupItem>();
            foreach (var function in functions)
            {
                if (typeElement.Methods.Any(m => function.Match(m) != EventFunctionMatch.NoMatch))
                    continue;

                items.Clear();

                // TODO: Decide what to do with e.g. `void OnAnima{caret}`
                // If we want to insert a visibility modifier, it has to go *before* the `void`,
                // which means adding a behaviour here that will remove it
                if (!hasVisibilityModifier && !hasReturnType)
                {
                    var factory = CSharpLookupItemFactory.Instance;
                    var lookupItem = factory.CreateTextLookupItem(context.BasicContext, context.CompletionRanges,
                        "private ");
                    items.Add(lookupItem);
                }

                var item = CreateMethodItem(context, function, classDeclaration);
                if (item == null) continue;

                items.Add(item);

                item = CombineLookupItems(context.BasicContext, context.CompletionRanges, items, item)
                    .WithHighSelectionPriority();

                // When items are sorted by relevance, highest value wins. But relevance is a mix of language
                // specific values plus matching, plus evaluation order. Light has higher relevance than Full.
                // Secondary sort is OrderString if relevance matches. This is usually display text, but can
                // be overridden. When items are sorted lexicographically, first sort is Location, then Rank,
                // then OrderString.
                // This, coupled with the fact that we're in Light evaluation mode, is enough to put us to the
                // top of the code completion window
                item.Placement.Relevance |= (long) (CLRLookupItemRelevance.GenerateItems | CLRLookupItemRelevance.Methods);
                if (function.Undocumented)
                    item.PlaceBottom();
                else
                    item.PlaceTop();

                collector.Add(item);
            }

            return true;
        }

        private ILookupItem CombineLookupItems(CodeCompletionContext basicContext, TextLookupRanges completionRanges, List<ILookupItem> displayItems, ILookupItem sampleMatchItem)
        {
            // Use a combined lookup item list to add "private " to the start of the display text.
            // We could probably get the same effect with a customised ILookupItemPresentation.
            // Make sure the completion lookup item list is different to the display lookup item
            // list or we get errors in highlighting matches - see RSRP-466980
            var matchingItems = new[] {sampleMatchItem};
            var item = new CombinedLookupItem(matchingItems, displayItems);
            item.InitializeRanges(completionRanges, basicContext);
            return item;
        }

        private static ILookupItem CreateMethodItem(CSharpCodeCompletionContext context, UnityEventFunction eventFunction,
            IClassLikeDeclaration declaration)
        {
            if (CSharpLanguage.Instance == null)
                return null;

            var method = eventFunction.CreateDeclaration(CSharpElementFactory.GetInstance(declaration), declaration);
            if (method.DeclaredElement == null)
                return null;

            var instance = new DeclaredElementInstance(method.DeclaredElement);

            var declaredElementInfo = new DeclaredElementInfo(method.DeclaredName, instance, CSharpLanguage.Instance,
                context.BasicContext.LookupItemsOwner, context)
            {
                Ranges = context.CompletionRanges
            };

            var withMatcher = LookupItemFactory.CreateLookupItem(declaredElementInfo).
                WithPresentation(_ => new GenerateMemberPresentation(declaredElementInfo, PresenterStyles.DefaultPresenterStyle)).
                WithBehavior(_ => new UnityEventFunctionBehavior(declaredElementInfo, eventFunction)).
                WithMatcher(_ => new DeclaredElementMatcher(declaredElementInfo, context.BasicContext.IdentifierMatchingStyle));
            return withMatcher;
        }

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
            hasVisibilityModifier = HasExisitingVisibilityModifier(typeUsage);
            classDeclaration = GetClassDeclaration(context.NodeInFile);

            return true;
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
                if (!fieldDeclaration.FixedBufferSizeExpression.IsConstantValue())
                    return identifier == fieldDeclaration.NameIdentifier;
            }

            return false;
        }

        private static bool HasExistingReturnType(ITreeNode identifier, out ITypeUsage typeUsage)
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

        private static bool HasExisitingVisibilityModifier(ITreeNode typeUsage)
        {
            if (!(typeUsage.GetPreviousMeaningfulSibling() is IModifiersList modifiersList))
                return false;

            // TODO: What about virtual or override?
            return (modifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                    modifiersList.HasModifier(CSharpTokenType.INTERNAL_KEYWORD) ||
                    modifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD) ||
                    modifiersList.HasModifier(CSharpTokenType.PRIVATE_KEYWORD));
        }

        private static IClassLikeDeclaration GetClassDeclaration(ITreeNode completionNode)
        {
            return completionNode.GetContainingNode<IClassLikeDeclaration>();
        }
    }
}