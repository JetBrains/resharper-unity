using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class PerformanceLineMarkerAnalyzer : PerformanceProblemAnalyzerBase<IFunctionDeclaration>
    {
        private readonly SettingsScalarEntry myLineMarkerEntry;
        private readonly IContextBoundSettingsStoreLive mySettingsStore;

        public PerformanceLineMarkerAnalyzer(Lifetime lifetime, ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
            myLineMarkerEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings key) => key.PerformanceHighlightingMode);
        }

        protected sealed override void Analyze(IFunctionDeclaration functionDeclaration,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (mySettingsStore.GetValue(myLineMarkerEntry, null) is PerformanceHighlightingMode.Always)
            {
                consumer.AddHighlighting(
                    new UnityPerformanceCriticalCodeLineMarker(GetHighlightRange(functionDeclaration)));
            }
        }

        protected virtual DocumentRange GetHighlightRange(IFunctionDeclaration functionDeclaration)
        {
            // As always, ReSharper behaviour is the default, and we override with Rider. This makes code and testing
            // easier. We can avoid having Unity.Tests, Unity.Rider.Tests and Unity.VisualStudio.Tests
            return functionDeclaration.GetNameDocumentRange();
        }
    }
}