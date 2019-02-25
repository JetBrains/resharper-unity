using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [SolutionComponent]
    public class ExpensiveCodeCallGraphAnalyzer : CallGraphAnalyzerBase
    {
        public const string MarkId = "Unity.ExpensiveCode";

        public ExpensiveCodeCallGraphAnalyzer(Lifetime lifetime, ISolution solution,
            UnitySolutionTracker unitySolutionTracker, ICallGraphAnalyzersProvider provider)
            : base(lifetime, provider, MarkId, new CalleeToCallerCallGraphPropagator(solution, MarkId))
        {
            Enabled.Value = unitySolutionTracker.IsUnityProject.HasTrueValue();
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