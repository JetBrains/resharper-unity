using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class CameraMainUsageAnalyzer : PerformanceProblemAnalyzerBase<IReferenceExpression>
    {
        protected override void Analyze(IReferenceExpression referenceExpression,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (PerformanceCriticalCodeStageUtil.IsCameraMainUsage(referenceExpression))
            {
                consumer.AddHighlighting(new UnityPerformanceCameraMainWarning(referenceExpression));
            }
        }
    }
}