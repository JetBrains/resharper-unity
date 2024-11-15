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
    public sealed class ExpensiveInvocationContextProvider : CallGraphContextProviderBase
    {
        private readonly IProperty<bool> myIsPerformanceAnalysisEnabledProperty;

        public ExpensiveInvocationContextProvider(
            Lifetime lifetime,
            IShellLocks shellLocks,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
            myIsPerformanceAnalysisEnabledProperty =
                applicationWideContextBoundSettingStore.BoundSettingsStore.GetValueProperty2(lifetime,
                    (UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting, ApartmentForNotifications.Primary(shellLocks));
        }

        public override bool IsContextAvailable => myIsPerformanceAnalysisEnabledProperty.Value;

        public override CallGraphContextTag ContextTag => CallGraphContextTag.EXPENSIVE_CONTEXT;
    }
}