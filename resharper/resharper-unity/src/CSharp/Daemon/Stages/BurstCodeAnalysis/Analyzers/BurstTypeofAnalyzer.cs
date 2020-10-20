using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstTypeofAnalyzer : BurstProblemAnalyzerBase<ITypeofExpression>
    {
        protected override bool CheckAndAnalyze(ITypeofExpression typeofExpression, IHighlightingConsumer consumer)
        {
            consumer?.AddHighlighting(new BurstTypeofExpressionWarning(typeofExpression));
            return true;
        }
    }
}