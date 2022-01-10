using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class PerformanceLineMarkerAnalyzer : PerformanceProblemAnalyzerBase<IFunctionDeclaration>
    {
        protected readonly IProperty<PerformanceHighlightingMode> LineMarkerStatus;

        public PerformanceLineMarkerAnalyzer(Lifetime lifetime, IApplicationWideContextBoundSettingStore settingsStore)
        {
            LineMarkerStatus = settingsStore.BoundSettingsStore
                .GetValueProperty(lifetime, (UnitySettings key) => key.PerformanceHighlightingMode);
        }

        protected override void Analyze(IFunctionDeclaration t,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (LineMarkerStatus.Value == PerformanceHighlightingMode.Always)
            {
                // As always, ReSharper behaviour is the default, and we override with Rider. This makes code and
                // testing easier. We can avoid having Unity.Tests, Unity.Rider.Tests and Unity.VisualStudio.Tests
                consumer.AddHighlighting(new UnityPerformanceCriticalCodeLineMarker(t.GetNameDocumentRange()));
            }
        }
    }
}