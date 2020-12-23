using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class NullComparisonWithUnityObjectProblemAnalyzer : PerformanceProblemAnalyzerBase<IEqualityExpression>
    {
        protected override void Analyze(IEqualityExpression equalityExpression,
            IHighlightingConsumer consumer, IReadOnlyContext context)
        {
            if (PerformanceCriticalCodeStageUtil.IsNullComparisonWithUnityObject(equalityExpression, out var name))
                consumer.AddHighlighting(new UnityPerformanceNullComparisonWarning(equalityExpression, name, equalityExpression.Reference.NotNull("eqaulityReference != null")));
        }
    }
}