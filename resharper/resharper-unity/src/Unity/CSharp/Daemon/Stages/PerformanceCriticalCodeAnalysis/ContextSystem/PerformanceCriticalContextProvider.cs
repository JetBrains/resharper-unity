using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public sealed class PerformanceCriticalContextProvider : CallGraphContextProviderBase
    {
        private readonly IProperty<bool> myIsPerformanceAnalysisEnabledProperty;

        public PerformanceCriticalContextProvider(
            Lifetime lifetime,
            IShellLocks shellLocks,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            PerformanceCriticalCodeMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
            myIsPerformanceAnalysisEnabledProperty =
                applicationWideContextBoundSettingStore.BoundSettingsStore.GetValueProperty2(lifetime,
                    (UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting, ApartmentForNotifications.Primary(shellLocks));
        }

        public override CallGraphContextTag ContextTag => CallGraphContextTag.PERFORMANCE_CRITICAL_CONTEXT;
        public override bool IsContextAvailable => myIsPerformanceAnalysisEnabledProperty.Value;
    }
}