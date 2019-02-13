using System.Linq;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    [ShellComponent]
    public class ExpensiveCodeCallGraphAnalyzer : ICallGraphElementAnalyzer
    {
        public const string MarkId = "ExpensiveCode";

        public ICallGraphPropagator CreatePropagator(ISolution solution) =>
            new CallingToCallerCallGraphPropagator(solution, MarkId);

        public string GetMarkId() => MarkId;

        public IDeclaredElement Mark(ITreeNode currentNode)
        {
            var isExpensive = false;
            switch (currentNode)
            {
                case IInvocationExpression invocationExpression when PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpression):
                case IReferenceExpression referenceExpression when PerformanceCriticalCodeStageUtil.IsCameraMainUsage(referenceExpression):
                case IEqualityExpression equalityExpressionParam when PerformanceCriticalCodeStageUtil.IsNullComparisonWithUnityObject(equalityExpressionParam, out _):
                    isExpensive = true;
                    break;
            }

            if (isExpensive)
                return ((ICSharpTreeNode) currentNode).GetContainingFunctionLikeDeclarationOrClosure()?.DeclaredElement;
            return null;
        }
    }
}