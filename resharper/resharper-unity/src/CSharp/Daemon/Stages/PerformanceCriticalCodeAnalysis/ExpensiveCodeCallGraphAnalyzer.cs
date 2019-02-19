using System.Linq;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [SolutionComponent]
    public class ExpensiveCodeCallGraphAnalyzer : CallGraphFunctionAnalyzerBase
    {
        public const string MarkId = "Unity.ExpensiveCode";

        public ExpensiveCodeCallGraphAnalyzer(Lifetime lifetime, ISolution solution, ICallGraphFunctionAnalyzersProvider provider)
            : base(lifetime, provider, MarkId, new CalleeToCallerCallGraphPropagator(solution, MarkId))
        {
        }
        
        public override LocalList<IDeclaredElement> GetMarkedFunctionsFrom(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();
            switch (currentNode)
            {
                case IInvocationExpression invocationExpression when PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpression):
                case IReferenceExpression referenceExpression when PerformanceCriticalCodeStageUtil.IsCameraMainUsage(referenceExpression):
                case IEqualityExpression equalityExpressionParam when PerformanceCriticalCodeStageUtil.IsNullComparisonWithUnityObject(equalityExpressionParam, out _):
                    result.Add(containingFunction);
                    break;
            }

            return result;
        }
    }
}