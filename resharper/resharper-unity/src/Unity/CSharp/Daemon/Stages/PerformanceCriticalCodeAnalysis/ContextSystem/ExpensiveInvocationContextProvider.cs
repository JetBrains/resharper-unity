using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class ExpensiveInvocationContextProvider : CallGraphContextProviderBase
    {
        private readonly SettingsScalarEntry myIsPerformanceAnalysisEnabledProperty;
        private readonly IContextBoundSettingsStoreLive mySettingsStore;

        public ExpensiveInvocationContextProvider(
            Lifetime lifetime,
            ISettingsStore settingsStore,
            IElementIdProvider elementIdProvider,
            CallGraphSwaExtensionProvider callGraphSwaExtensionProvider,
            ExpensiveCodeMarksProvider marksProviderBase)
            : base(elementIdProvider, callGraphSwaExtensionProvider, marksProviderBase)
        {
            mySettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
            myIsPerformanceAnalysisEnabledProperty = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) =>
                s.EnablePerformanceCriticalCodeHighlighting);
        }

        public override bool IsContextAvailable =>
            mySettingsStore.GetValue(myIsPerformanceAnalysisEnabledProperty, null) is true;

        public override CallGraphContextTag ContextTag => CallGraphContextTag.EXPENSIVE_CONTEXT;
    }
}