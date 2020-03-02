using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstCSharpArgumentAnalyzer : BurstProblemAnalyzerBase<ICSharpArgument>
    {
        protected override void Analyze(ICSharpArgument argument, IDaemonProcess daemonProcess, DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            if (!(argument.MatchingParameter?.Type.IsSuitableForBurst() ?? true))
            {
                consumer.AddHighlighting(new BurstWarning(argument.GetDocumentRange() ,
                    $"parameter {argument.IndexOf()} is managed object"));
            }
        }
    }
}