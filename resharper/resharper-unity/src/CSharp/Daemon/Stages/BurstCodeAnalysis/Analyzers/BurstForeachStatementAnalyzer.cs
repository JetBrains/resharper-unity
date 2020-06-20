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
            //juding by latest documentation, foreach is prohibited to use.
            //burst compiler does not allow to use it either, even with struct enumerator
            //despite all of that, foreach is used in unity sources with burst ¯\_(ツ)_/¯
            consumer?.AddHighlighting(new BC1037Error(foreachStatement.ForeachKeyword.GetDocumentRange()));
            return true;
        }
    }
}