using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstFunctionSignatureAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression,
            IHighlightingConsumer consumer)
        {
            var invokedMethod = CallGraphUtil.GetCallee(invocationExpression) as IMethod;

            if (invokedMethod == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            if (!IsReturnValueBurstProhibited(invokedMethod) && !HasBurstProhibitedArguments(invocationExpression.ArgumentList))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            
            consumer?.AddHighlighting(new BurstFunctionSignatureContainsManagedTypesWarning(invocationExpression, invokedMethod.ShortName));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 4000;
    }
}