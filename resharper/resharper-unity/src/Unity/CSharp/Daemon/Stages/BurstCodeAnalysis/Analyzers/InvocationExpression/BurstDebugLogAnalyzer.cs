using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BurstDebugLogAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;

            if (invokedMethod == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            if (!IsDebugLog(invokedMethod))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            var argumentList = invocationExpression.ArgumentList.Arguments;

            if (argumentList.Count != 1)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            var argument = argumentList[0];

            if (argument == null || IsBurstPossibleArgumentString(argument))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            consumer?.AddHighlighting(new BurstDebugLogInvalidArgumentWarning(argument.Expression));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 1000;
    }
}