using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class PerformanceLineMarkerAnalyzer : PerformanceProblemAnalyzerBase<IFunctionDeclaration>
    {
        protected readonly IProperty<PerformanceHighlightingMode> LineMarkerStatus;
        public PerformanceLineMarkerAnalyzer(Lifetime lifetime, ISolution solution, ISettingsStore store)
        {
            LineMarkerStatus = store.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
                .GetValueProperty(lifetime, (UnitySettings key) => key.PerformanceHighlightingMode);
        }
        
        protected override void Analyze(IFunctionDeclaration t, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            if (LineMarkerStatus.Value == PerformanceHighlightingMode.Always)
            {
                consumer.AddHighlighting(new PerformanceHighlighting(t.GetDocumentRange()));
            }
        }
    }
}