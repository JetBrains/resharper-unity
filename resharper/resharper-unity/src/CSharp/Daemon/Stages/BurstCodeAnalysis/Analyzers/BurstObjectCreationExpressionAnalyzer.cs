using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Psi;
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
            //CGTD not getContaningParenthesized, i have to figure out throw new Exception(new object().ToString());
            if (!objectCreationExpression.Type().IsSuitableForBurst() && !(objectCreationExpression.GetContainingParenthesizedExpression().Parent is IThrowStatement))
            {
                consumer.AddHighlighting(new BC1021Error(objectCreationExpression.GetDocumentRange(), (objectCreationExpression.ConstructorReference.Resolve().DeclaredElement as IConstructor)?.GetContainingType()?.ShortName));
            }
        }
    }
}