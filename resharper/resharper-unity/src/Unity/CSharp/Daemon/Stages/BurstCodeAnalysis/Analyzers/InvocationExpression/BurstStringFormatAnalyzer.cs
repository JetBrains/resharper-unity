using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstStringFormatAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;

            if (invokedMethod == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            if (!IsStringFormat(invokedMethod))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var argumentList = invocationExpression.ArgumentList.Arguments;

            if (argumentList.Count == 0)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            var firstArgument = argumentList[0];
            var cSharpLiteralExpression = firstArgument.Expression as ICSharpLiteralExpression;

            if (cSharpLiteralExpression != null && cSharpLiteralExpression.Literal.GetTokenType().IsStringLiteral)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            consumer?.AddHighlighting(new BurstStringFormatInvalidArgumentWarning(firstArgument.Expression));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 2000;
    }
}