using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public sealed class BurstTryStatementAnalyzer : BurstProblemAnalyzerBase<ITryStatement>
    {
        protected override bool CheckAndAnalyze(ITryStatement tryStatement, IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            consumer?.AddHighlighting(new BurstTryNotSupportedWarning(tryStatement.TryKeyword));
            return true;
        }
    }
}