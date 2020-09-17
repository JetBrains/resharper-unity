using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class ExpensiveInvocationContextProvider : UnityProblemAnalyzerContextProviderBase
    {
        public ExpensiveInvocationContextProvider(IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
        }

        public override bool IsEnabled => false;

        public override UnityProblemAnalyzerContextElement Context =>
            UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT;

        protected override bool HasContextFast(ITreeNode treeNode) =>
            treeNode is IInvocationExpression invocationExpression &&
            PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpression);

        protected override bool IsMarkedFast(IDeclaredElement declaredElement) =>
            PerformanceCriticalCodeStageUtil.IsInvokedElementExpensive(declaredElement as IMethod);

        protected override bool IsBannedFast(IDeclaredElement declaredElement) => false;
        protected override bool IsContextProhibitedFast(ITreeNode treeNode) => false;
    }
}