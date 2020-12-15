using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public sealed class PerformanceCriticalContextProvider : PerformanceAnalysisContextProviderBase
    {
        public PerformanceCriticalContextProvider(
            Lifetime lifetime,
            IElementIdProvider elementIdProvider,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeMarksProvider marksProvider,
            SolutionAnalysisService service)
            : base(lifetime, elementIdProvider,
                applicationWideContextBoundSettingStore, callGraphSwaExtensionProvider,
                marksProvider, service)
        {
        }

        public override CallGraphContextElement Context => CallGraphContextElement.PERFORMANCE_CRITICAL_CONTEXT;

        protected override bool CheckDeclaredElement(IDeclaredElement element, out bool isMarked)
        {
            if (PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(element))
            {
                isMarked = true;
                return true;
            }

            return base.CheckDeclaredElement(element, out isMarked);
        }
    }
}