using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstSharedStaticCreateAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public int Priority => 4000;

        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression, IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;

            if (!BurstCodeAnalysisUtil.IsSharedStaticCreateMethod(invokedMethod))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            if (invokedMethod.TypeParameters.Count != 0)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            var methodParameters = invokedMethod.Parameters;

            for (var index = 0; index < methodParameters.Count - 1; index++)
            {
                if (!methodParameters[index].Type.IsSystemType())
                    return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;
            }

            consumer?.AddHighlighting(new BurstSharedStaticCreateWarning(invocationExpression));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }
    }
}