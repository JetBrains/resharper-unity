using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public sealed class BurstTypeofAnalyzer : BurstProblemAnalyzerBase<ITypeofExpression>, IBurstBannedAnalyzer
    {
        protected override bool CheckAndAnalyze(ITypeofExpression typeofExpression, IHighlightingConsumer consumer,
            IReadOnlyCallGraphContext context)
        {
            consumer?.AddHighlighting(new BurstTypeofExpressionWarning(typeofExpression));
            return true;
        }
    }
}