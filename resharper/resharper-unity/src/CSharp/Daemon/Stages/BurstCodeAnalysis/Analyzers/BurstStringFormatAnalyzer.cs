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
    public class BurstStringFormatAnalyzer : BurstProblemAnalyzerBase<IInvocationExpression>
    {
        protected override bool CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = CallGraphUtil.GetCallee(invocationExpression) as IMethod;

            if (invokedMethod == null)
                return false;

            if (!IsStringFormat(invokedMethod))
                return false;

            var argumentList = invocationExpression.ArgumentList.Arguments;

            var isWarningPlaced = BurstStringLiteralOwnerAnalyzer.CheckAndAnalyze(invocationExpression,
                new BurstManagedStringWarning(invocationExpression.GetDocumentRange()), consumer);

            if (isWarningPlaced)
                return true;

            if (argumentList.Count == 0)
                return false;

            var firstArgument = argumentList[0];
            var cSharpLiteralExpression = firstArgument.Expression as ICSharpLiteralExpression;

            if (cSharpLiteralExpression != null && cSharpLiteralExpression.Literal.GetTokenType().IsStringLiteral)
                return false;

            consumer?.AddHighlighting(
                new BurstDebugLogInvalidArgumentWarning(firstArgument.Expression.GetDocumentRange()));

            return true;
        }
    }
}