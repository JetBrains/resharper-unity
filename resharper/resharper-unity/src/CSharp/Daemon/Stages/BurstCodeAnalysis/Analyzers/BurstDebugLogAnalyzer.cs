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
    public class BurstDebugLogAnalyzer : BurstProblemAnalyzerBase<IInvocationExpression>
    {
        protected override bool CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = CallGraphUtil.GetCallee(invocationExpression) as IMethod;

            if (invokedMethod == null)
                return false;

            if (!IsDebugLog(invokedMethod))
                return false;

            var argumentList = invocationExpression.ArgumentList.Arguments;

            if (argumentList.Count != 1)
                return false;

            var argument = argumentList[0];

            if (IsBurstPermittedString(argument.Expression?.Type()))
                return false;

            consumer?.AddHighlighting(new BurstDebugLogInvalidArgumentWarning(argument.Expression.GetDocumentRange()));

            return true;
        }
    }
}