using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class CameraMainUsageAnalyzer : PerformanceProblemAnalyzerBase<IReferenceExpression>
    {
        protected override void Analyze(IReferenceExpression referenceExpression, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            if (PerformanceCriticalCodeStageUtil.IsCameraMainUsage(referenceExpression))
            {
                consumer.AddHighlighting(new PerformanceCameraMainHighlighting(referenceExpression));
            }
        }
    }
}