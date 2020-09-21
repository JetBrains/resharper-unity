using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public class PerformanceCriticalCodeContextProvider : UnityProblemAnalyzerContextProviderBase
    {
        public override UnityProblemAnalyzerContextElement Context =>
            UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT;

        protected override bool HasContextFast(ITreeNode treeNode)
        {
            return PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(treeNode);
        }

        protected override bool IsMarkedFast(IDeclaredElement declaredElement)
        {
            return PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(declaredElement);
        }

        protected override bool IsBannedFast(IDeclaredElement declaredElement) => false;

        protected override bool IsContextProhibitedFast(ITreeNode treeNode) => false;

        public PerformanceCriticalCodeContextProvider(IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeMarksProvider marksProvider)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProvider)
        {
        }
    }
}