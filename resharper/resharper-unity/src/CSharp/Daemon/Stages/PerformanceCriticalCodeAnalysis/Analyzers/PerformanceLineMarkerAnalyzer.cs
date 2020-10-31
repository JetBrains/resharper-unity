using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class PerformanceLineMarkerAnalyzer : PerformanceProblemAnalyzerBase<IFunctionDeclaration>
    {
        protected readonly IProperty<PerformanceHighlightingMode> LineMarkerStatus;

        public PerformanceLineMarkerAnalyzer(Lifetime lifetime, ISolution solution,
                                             IApplicationWideContextBoundSettingStore settingsStore)
        {
            LineMarkerStatus = settingsStore.BoundSettingsStore
                .GetValueProperty(lifetime, (UnitySettings key) => key.PerformanceHighlightingMode);
        }

        protected override void Analyze(IFunctionDeclaration t, IDaemonProcess daemonProcess, DaemonProcessKind kind,
                                        IHighlightingConsumer consumer)
        {
            if (LineMarkerStatus.Value == PerformanceHighlightingMode.Always)
            {
                consumer.AddHighlighting(new UnityPerformanceCriticalCodeLineMarker(t.GetDocumentRange()));
            }
        }
    }
}