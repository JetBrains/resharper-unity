using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.BurstCodeAnalysisUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class BurstObjectMethodInvocationAnalyzer : IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(
            IInvocationExpression invocationExpression, IHighlightingConsumer consumer)
        {
            var invokedMethod = invocationExpression.Reference.Resolve().DeclaredElement as IMethod;

            if (invokedMethod == null || UnityCallGraphUtil.IsQualifierOpenType(invocationExpression))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_STOP;

            if (!IsBurstProhibitedObjectMethod(invokedMethod))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;

            consumer?.AddHighlighting(new BurstAccessingManagedMethodWarning(invocationExpression,
                invokedMethod.ShortName, invokedMethod.ContainingType?.ShortName));

            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 3000;
    }
}