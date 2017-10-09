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
            var projectPsiModule = context.PsiModule as IProjectPsiModule;
            if (projectPsiModule == null || !projectPsiModule.Project.IsUnityProject())
            {
                return false;
            }

            var completionTypes = context.BasicContext.Parameters.CodeCompletionTypes;

            // Don't show for import completion
            if (completionTypes.Length == 1 && completionTypes[0] != CodeCompletionType.BasicCompletion
                && completionTypes[0] != CodeCompletionType.SmartCompletion)
            {
                return false;
            }

            // If double completion, only allow double basic completion
            if (completionTypes.Length == 2 &&
                (completionTypes[0] != CodeCompletionType.BasicCompletion ||
                 completionTypes[1] != CodeCompletionType.BasicCompletion))
            {
                return false;
            }

            // Only double completion, no more
            if (completionTypes.Length < 1 || completionTypes.Length > 2)
                return false;

            return !context.IsQualified;
        }

        public override CompletionMode SupportedCompletionMode => CompletionMode.All;
        public override EvaluationMode SupportedEvaluationMode => EvaluationMode.LightAndFull;

        protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
        {
            if (!CheckPosition(context, out var classDeclaration, out var hasVisibilityModifier, out var hasReturnType))
                return false;

            // Don't add anything in double completion - we've already added it
            // TODO: Confused here. Why do we allow double completion in IsAvailable, but not do anything with it here?
            if (context.BasicContext.Parameters.Multiplier > 1)
                return true;

            // Don't add anything in the light evaluation pass
            if (context.BasicContext.Parameters.EvaluationMode == EvaluationMode.Light)
                return false;

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

                item = CombineLookupItems(context.BasicContext, context.CompletionRanges, items, item);

                item.Placement.Relevance |= (long) CLRLookupItemRelevance.GenerateItems;
                if (function.Undocumented)
                    item.Placement.Location = PlacementLocation.Bottom;

                collector.Add(item);
            }

            return true;
        }

        private ILookupItem CombineLookupItems(CodeCompletionContext basicContext, TextLookupRanges completionRanges, List<ILookupItem> items, ILookupItem sampleMatchItem)
        {
            var item = new CombinedLookupItem(items.ToArray(), items, sampleMatchItem, autocomplete: true, exactMatch: items.Count > 1);
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
                context.BasicContext.LookupItemsOwner, context);

            return LookupItemFactory.CreateLookupItem(declaredElementInfo).
                WithPresentation(
                    _ => new GenerateMemberPresentation(declaredElementInfo, PresenterStyles.DefaultPresenterStyle)).
                WithBehavior(_ =>
                {
                    var behavior = new UnityEventFunctionBehavior(declaredElementInfo, eventFunction);
                    return behavior;
                }).
                WithMatcher(_ => new DeclaredElementMatcher(declaredElementInfo, context.BasicContext.IdentifierMatchingStyle));
        }

        private bool CheckPosition(CSharpCodeCompletionContext context, out IClassLikeDeclaration declaration,
            out bool hasVisibilityModifier, out bool hasReturnType)
        {
            declaration = null;
            hasVisibilityModifier = false;
            hasReturnType = false;

            // Are we trying to complete an identifier?
            var identifier = context.UnterminatedContext.TreeNode as ICSharpIdentifier;
            if (identifier == null || identifier.GetContainingNode<IForeachStatement>() != null)
            {
                return false;
            }

            // If we haven't typed a return type, the identifier will be parsed
            // as a type usage. Get its parent (visibility modifier is separate)
            var typeUsage = identifier.GetContainingNode<ITypeUsage>();
            if (typeUsage == null)
            {
                var node = identifier.PrevSibling;
                while (node != null && node.IsWhitespaceToken())
                    node = node.PrevSibling;
                if (node != null && !(node is ITypeUsage))
                    return false;
                typeUsage = (ITypeUsage) node;

                if (typeUsage == null)
                    return false;

                // TODO: This is working around `void OnAnima{caret}`
                // This allows us to not add the `private ` modifier, because that would
                // be added *after* the `void` instead of `before`
                hasReturnType = true;
            }

            // Preceding access modifier (public, private, etc)
            // If there isn't a modifier, there won't be a significant node
            var treeNode = typeUsage.PrevSibling;
            while (treeNode != null && treeNode.IsWhitespaceToken())
                treeNode = treeNode.PrevSibling;
            if (treeNode != null && !(treeNode is IModifiersList))
                return false;

            var methodDeclaration = typeUsage.GetContainingNode<IMethodDeclaration>();
            if (methodDeclaration == null)
                return false;

            // Check if this is a method declaration, e.g. the user is typing
            // the return type
            var methodName = methodDeclaration.GetText();
            if (!methodName.EndsWith("__") && !methodName.StartsWith("__"))
                return false;

            // TODO: What about virtual or override?
            var modifiersList = treeNode as IModifiersList;
            hasVisibilityModifier = modifiersList != null &&
                                    (modifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                                     modifiersList.HasModifier(CSharpTokenType.INTERNAL_KEYWORD) ||
                                     modifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD) ||
                                     modifiersList.HasModifier(CSharpTokenType.PRIVATE_KEYWORD));

            var nodeInFile = context.NodeInFile;
            if (nodeInFile == null)
                return false;

            // Don't include identifiers in the parameter list
            var formalParameterList = nodeInFile.GetContainingNode<IFormalParameterList>();
            if (formalParameterList != null)
                return false;

            var classBody = nodeInFile.GetContainingNode<IClassBody>();
            if (classBody == null)
                return false;

            declaration = classBody.Parent as IClassLikeDeclaration;
            if (declaration == null)
                return false;

            // Wouldn't this be covered by GetContainingNode<IFormalParameterList> above?
            treeNode = nodeInFile;
            while (treeNode is IWhitespaceNode)
                treeNode = treeNode.PrevSibling;
            if (treeNode == null || treeNode is IFormalParameterList)
                return false;

            return true;
        }
    }
}