using JetBrains.Annotations;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

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
            // CGTD overlook. which states of swea is ok
            return IsSweaCompleted(solutionAnalysisService)
                ? DaemonProcessKind.GLOBAL_WARNINGS
                : DaemonProcessKind.VISIBLE_DOCUMENT;
        }

        public static bool IsSweaCompleted([NotNull] SolutionAnalysisService solutionAnalysisService)
        {
            return solutionAnalysisService.Configuration?.Enabled?.Value == true;
        }

        public const string PerformanceExpensiveComment = "Unity.CG";

        [CanBeNull] [Pure]
        public static IDeclaredElement HasAnalysisComment(ITreeNode node, string comment, ReSharperControlConstruct.Kind status)
        {
            var methodDeclaration = node as IMethodDeclaration;

            if (methodDeclaration == null)
                return null;

            var current = methodDeclaration.PrevSibling;

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
                                return methodDeclaration.DeclaredElement;
                        }
                    }

                    current = current.PrevSibling;
                }
            }
                    
            // 1. ban has more respect than not ban
            // 2. every marks provider should have their respectful comments
            // 3. every comment should have their at least context actions
            // how? 
            // static function that accepts treenode and string analysis name. string should be interned
            // and overloads for every marks provider with consts
            // w2d also? like if -> the only thing that came up to my mind is new abstract class that would override
            // but screw it, too hard no use, someday. stop dealing with tech debt every time you have one
            // let it accumulate and deal one time, creating new structure that would lessen amount of useless refactoring

            return null;
        }
    }
}