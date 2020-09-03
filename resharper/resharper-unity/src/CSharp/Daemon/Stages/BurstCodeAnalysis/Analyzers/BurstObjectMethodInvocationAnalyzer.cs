using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstObjectMethodInvocationAnalyzer : BurstProblemAnalyzerBase<IInvocationExpression>
    {
        protected override bool CheckAndAnalyze(IInvocationExpression invocationExpression, IHighlightingConsumer consumer)
        {
            var invokedMethod = CallGraphUtil.GetCallee(invocationExpression) as IMethod;

            if (invokedMethod == null)
                return false;

            if (!IsObjectMethodInvocation(invocationExpression))
                return false;
            
            consumer?.AddHighlighting(new BurstAccessingManagedMethodWarning(invocationExpression.GetDocumentRange(),
                invokedMethod.ShortName, invokedMethod.GetContainingType()?.ShortName));

            return true;
        }
    }
}