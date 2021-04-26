using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstFunctionSignatureAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IParametersOwner;

            if (invokedMethod == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            var argumentList = invocationExpression.ArgumentList;

            if (HasBurstProhibitedReturnValue(invokedMethod) ||
                argumentList != null && HasBurstProhibitedArguments(invocationExpression.ArgumentList))
            {
                consumer?.AddHighlighting(
                    new BurstFunctionSignatureContainsManagedTypesWarning(invocationExpression, invokedMethod.ShortName));

                return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            }

            return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
        }

        public int Priority => 5000;
    }
}