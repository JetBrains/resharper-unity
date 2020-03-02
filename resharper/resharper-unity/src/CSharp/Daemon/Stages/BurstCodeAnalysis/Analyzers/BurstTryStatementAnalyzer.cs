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
        protected override void Analyze(ITryStatement tryStatement, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            consumer.AddHighlighting(new BurstWarning(tryStatement.TryKeyword.GetDocumentRange(), "try statements"));
            consumer.AddHighlighting(new BurstWarning(tryStatement.FinallyKeyword.GetDocumentRange(), "finally statements"));
        }
    }
}