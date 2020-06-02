using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstCatchClauseAnalyzer : BurstProblemAnalyzerBase<ICatchClause>
    {
        protected override void Analyze(ICatchClause catchClause, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            consumer.AddHighlighting(new BC1037Error(catchClause.CatchKeyword.GetDocumentRange()));
        }
    }
}