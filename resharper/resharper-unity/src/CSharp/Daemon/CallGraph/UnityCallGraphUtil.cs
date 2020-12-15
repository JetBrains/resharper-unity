using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph
{
    public static class UnityCallGraphUtil
    {
        [ContractAnnotation("null => false")]
        public static bool IsFunctionNode(ITreeNode node)
        {
            // CGTD use ICallHierarchyLanguageSpecific
            switch (node)
            {
                case IFunctionDeclaration _:
                case ICSharpClosure _:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsSweaCompleted([NotNull] SolutionAnalysisService solutionAnalysisService)
        {
            return solutionAnalysisService.Configuration?.Completed?.Value == true;
        }

        public const string PerformanceAnalysisComment = "Unity.PerformanceAnalysis";

        public static bool IsQualifierOpenType(IInvocationExpression invocationExpression)
        {
            var invokedReferenceExpressionQualifier = invocationExpression.GetInvokedReferenceExpressionQualifier();
            var qualifierType = invokedReferenceExpressionQualifier?.Type();

            return qualifierType?.IsOpenType == true;
        }

        /// <summary>
        /// This function is intended to be used at <see cref="CallGraphRootMarksProviderBase"/> and <see cref="CallGraphContextProviderBase"/>.
        /// If you use this function in other scope then you definitely doing something wrong.
        /// Consider using corresponding <see cref="CallGraphContextProviderBase"/>.
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <param name="comment"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [Pure]
        [ContractAnnotation("methodDeclaration: null => false")]
        public static bool HasAnalysisComment([CanBeNull] IMethodDeclaration methodDeclaration, string comment,
            ReSharperControlConstruct.Kind status)
        {
            if (methodDeclaration == null)
                return false;

            var current = methodDeclaration.PrevSibling;

            while (current is IWhitespaceNode || current is ICSharpCommentNode)
            {
                while (current is IWhitespaceNode)
                    current = current.PrevSibling;

                while (current is ICSharpCommentNode commentNode)
                {
                    var commentText = commentNode.CommentText;
                    var configuration = ReSharperControlConstruct.ParseCommentText(commentText);
                    var kind = configuration.Kind;

                    if (kind == status && configuration.GetControlIds().Any(id => id == comment))
                        return true;

                    current = current.PrevSibling;
                }
            }

            return false;
        }

        public static bool HasAnalysisComment(string markName, ITreeNode node, out bool isMarked)
        {
            if (node is IMethodDeclaration methodDeclaration)
            {
                const ReSharperControlConstruct.Kind restore = ReSharperControlConstruct.Kind.Restore;
                const ReSharperControlConstruct.Kind disable = ReSharperControlConstruct.Kind.Disable;

                if (HasAnalysisComment(methodDeclaration, markName, disable))
                {
                    isMarked = false;
                    return true;
                }


                if (HasAnalysisComment(methodDeclaration, markName, restore))
                {
                    isMarked = true;
                    return true;
                }
            }

            isMarked = false;
            return false;
        }

        [CanBeNull]
        public static IMethodDeclaration GetMethodDeclarationByIdentifierOnBothSides([NotNull] ICSharpContextActionDataProvider dataProvider)
        {
            var result = MethodDeclarationByTreeNode(dataProvider.TokenAfterCaret);

            return result ?? MethodDeclarationByTreeNode(dataProvider.TokenBeforeCaret);

            IMethodDeclaration MethodDeclarationByTreeNode(ITreeNode node)
            {
                var identifierAfter = node as ICSharpIdentifier;
                var methodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifierAfter);

                return methodDeclaration;
            }
        }

        [NotNull]
        public static BulbMenuItem BulbActionToMenuItem([NotNull] IBulbAction bulbAction,
                                                        [NotNull] ITextControl textControl,
                                                        [NotNull] ISolution solution,
                                                        [NotNull] IconId iconId)
        {
            var proxi = new IntentionAction.MyExecutableProxi(bulbAction, solution, textControl);
            var menuText = bulbAction.Text;
            var anchor = BulbMenuAnchors.FirstClassContextItems;
            var bulbMenuItem = new BulbMenuItem(proxi, menuText, iconId, anchor);

            return bulbMenuItem;
        }
    }
}