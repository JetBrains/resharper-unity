using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.VisualStudio
{
    [SolutionComponent]
    public class VSPerformanceLineMarkerAnalyzer : PerformanceLineMarkerAnalyzer
    {
        public VSPerformanceLineMarkerAnalyzer(Lifetime lifetime, ISolution solution, ISettingsStore store)
            : base(lifetime, solution, store)
        {
        }
        
        protected override void Analyze(IFunctionDeclaration t, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            if (LineMarkerStatus.Value == PerformanceHighlightingMode.Always)
            {
                consumer.AddHighlighting(new PerformanceHighlighting(t.GetNameDocumentRange()));
            }
        }
    }
}