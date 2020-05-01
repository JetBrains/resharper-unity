using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstStringLiteralOwnerAnalyzer : BurstProblemAnalyzerBase<ICSharpLiteralExpression>
    {
        protected override void Analyze(ICSharpLiteralExpression cSharpLiteralExpression, IDaemonProcess daemonProcess, DaemonProcessKind kind,
            IHighlightingConsumer consumer)
        {
            if (cSharpLiteralExpression.Literal.GetTokenType().IsStringLiteral)
            {
                consumer.AddHighlighting(new BC1033Error(cSharpLiteralExpression.GetDocumentRange()));
            }
        }
    }
}