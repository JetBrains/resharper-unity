using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph
{
    public abstract class PerformanceAnalysisRootMarksProviderBase : CallGraphCommentMarksProvider
    {
        protected PerformanceAnalysisRootMarksProviderBase(string name, ICallGraphPropagator propagator, bool isEnabled = true)
            : base(name, propagator, isEnabled)
        {
        }

        public static bool HasPerformanceBanComment(ITreeNode node)
        {
            return UnityCallGraphUtil.HasAnalysisComment(UnityCallGraphUtil.PerformanceAnalysisComment, node, out var isMarked) 
                   && !isMarked;
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            var result = base.GetBanMarksFromNode(currentNode, containingFunction);

            if (HasPerformanceBanComment(currentNode))
            {
                var methodDeclaration = currentNode as IMethodDeclaration;
                result.Add(methodDeclaration.DeclaredElement);
            }

            return result;
        }
    }
}