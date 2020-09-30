using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.InvocationExpression
{
    [SolutionComponent]
    public class BurstSharedStaticCreateAnalyzer: IBurstProblemSubAnalyzer<IInvocationExpression>
    {
        public BurstProblemSubAnalyzerStatus CheckAndAnalyze(IInvocationExpression invocationExpression, IHighlightingConsumer consumer)
        {
            var invokedMethod = CallGraphUtil.GetCallee(invocationExpression) as IMethod;
            var containingType = invokedMethod?.GetContainingType();
            var typeClrName = containingType?.GetClrName();
            
            if (typeClrName == null)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            
            if (!typeClrName.Equals(KnownTypes.SharedStatic))
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            
            if (invokedMethod.IsStatic == false)
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            
            if (invokedMethod.ShortName != "GetOrCreate")
                return BurstProblemSubAnalyzerStatus.NO_WARNING_CONTINUE;
            
            var parameters = invokedMethod.Parameters;
            
            if (parameters.Count != 2)
                return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            
            if (!parameters[1].IsOptional)
                return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            
            if (!parameters[1].Type.IsUint())
                return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
            
            if (!parameters[0].Type.IsSystemType())
                return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;

            consumer?.AddHighlighting(new BurstSharedStaticCreateMethodWarning(invocationExpression));
            
            return BurstProblemSubAnalyzerStatus.WARNING_PLACED_STOP;
        }

        public int Priority => 4000;
    }
}