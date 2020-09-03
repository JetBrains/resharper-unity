using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public class PerformanceCriticalCodeContextProvider : UnityProblemAnalyzerContextProviderBase
    {
        public override UnityProblemAnalyzerContextElement Context =>
            UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT;

        protected override bool IsRootFast(ICSharpDeclaration declaration)
        {
            return PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(declaration);
        }

        protected override bool IsProhibitedFast(ICSharpDeclaration declaration) => false;

        public PerformanceCriticalCodeContextProvider(IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeMarksProvider marksProvider)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProvider)
        {
        }
    }
}