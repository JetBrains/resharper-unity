using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstObjectCreationExpressionAnalyzer : BurstProblemAnalyzerBase<IObjectCreationExpression>
    {
        protected override void Analyze(IObjectCreationExpression objectCreationExpression, IDaemonProcess daemonProcess,
            DaemonProcessKind kind, IHighlightingConsumer consumer)
        {
            if (!objectCreationExpression.Type().IsSuitableForBurst() && !(objectCreationExpression.GetContainingParenthesizedExpression().Parent is IThrowStatement))
            {
                consumer.AddHighlighting(new BurstWarning(objectCreationExpression.GetDocumentRange(), "creating managed objects"));
            }
        }
    }
}