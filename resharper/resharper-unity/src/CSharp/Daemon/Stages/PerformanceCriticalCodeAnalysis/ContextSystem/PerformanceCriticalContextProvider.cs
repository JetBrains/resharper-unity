using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
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
            PerformanceCriticalCodeMarksProvider marksProvider)
            : base(lifetime, elementIdProvider, applicationWideContextBoundSettingStore, callGraphSwaExtensionProvider,
                marksProvider, PerformanceCriticalCodeMarksProvider.MarkId)
        {
        }

        public override CallGraphContextElement Context => CallGraphContextElement.PERFORMANCE_CRITICAL_CONTEXT;

        protected override bool IsMarkedFast(IDeclaredElement declaredElement) =>
            PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(declaredElement);
    }
}