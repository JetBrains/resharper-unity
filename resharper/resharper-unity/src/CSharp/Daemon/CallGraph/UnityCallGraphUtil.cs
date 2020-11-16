using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph
{
    public static class UnityCallGraphUtil
    {
        [ContractAnnotation("null => false")]
        public static bool IsFunctionNode(ITreeNode node)
        {
            switch (node)
            {
                case IFunctionDeclaration _:
                case ICSharpClosure _:
                    return true;
                default:
                    return false;
            }
        }

        public static DaemonProcessKind GetProcessKindForGraph(
            [NotNull] SolutionAnalysisService solutionAnalysisService)
        {
            return IsSweaCompleted(solutionAnalysisService)
                ? DaemonProcessKind.GLOBAL_WARNINGS
                : DaemonProcessKind.VISIBLE_DOCUMENT;
        }

        public static bool IsSweaCompleted([NotNull] SolutionAnalysisService solutionAnalysisService)
        {
            return solutionAnalysisService.Configuration?.Completed?.Value == true;
        }

        public const string PerformanceExpensiveComment = "Unity.PerformanceAnalysis";

        public static bool IsQualifierOpenType(IInvocationExpression invocationExpression)
        {
            var invokedReferenceExpressionQualifier = invocationExpression.GetInvokedReferenceExpressionQualifier();
            var qualifierType = invokedReferenceExpressionQualifier?.Type();

            return qualifierType?.IsOpenType == true;
        }

        [Pure]
        [ContractAnnotation("functionDeclaration: null => false")]
        public static bool HasAnalysisComment([CanBeNull] IFunctionDeclaration functionDeclaration, string comment, ReSharperControlConstruct.Kind status)
        {
            if (functionDeclaration == null)
                return false;

            var current = functionDeclaration.PrevSibling;

            while (current is IWhitespaceNode || current is ICSharpCommentNode)
            {
                while (current is IWhitespaceNode)
                    current = current.PrevSibling;

                while (current is ICSharpCommentNode commentNode)
                {
                    var str = commentNode.CommentText;
                    var configuration = ReSharperControlConstruct.ParseCommentText(str);

                    if (configuration.Kind == status)
                    {
                        foreach (var id in configuration.GetControlIds())
                        {
                            if (id == comment)
                                return true;
                        }
                    }

                    current = current.PrevSibling;
                }
            }

            return false;
        }
    }
}