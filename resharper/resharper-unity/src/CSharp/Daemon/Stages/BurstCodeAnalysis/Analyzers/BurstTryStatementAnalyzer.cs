using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstTryStatementAnalyzer : BurstProblemAnalyzerBase<ITryStatement>
    {
        protected override bool CheckAndAnalyze(ITryStatement tryStatement, IHighlightingConsumer consumer)
        {
            consumer?.AddHighlighting(new BC1005Error(tryStatement.TryKeyword.GetDocumentRange()));
            return true;
        }
    }
}