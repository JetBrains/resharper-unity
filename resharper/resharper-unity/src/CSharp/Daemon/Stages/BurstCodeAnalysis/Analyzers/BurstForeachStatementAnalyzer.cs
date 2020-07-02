using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstForeachStatementAnalyzer : BurstProblemAnalyzerBase<IForeachStatement>
    {
        protected override bool CheckAndAnalyze(IForeachStatement foreachStatement, IHighlightingConsumer consumer)
        {
            consumer?.AddHighlighting(new BurstForeachNotSupportedWarning(foreachStatement.ForeachKeyword.GetDocumentRange()));
            return true;
        }
    }
}