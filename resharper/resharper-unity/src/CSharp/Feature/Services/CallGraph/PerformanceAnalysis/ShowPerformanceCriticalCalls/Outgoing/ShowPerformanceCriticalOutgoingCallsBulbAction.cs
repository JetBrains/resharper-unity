using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls.Outgoing
{
    public class ShowPerformanceCriticalOutgoingCallsBulbAction: ShowPerformanceCriticalCallsBulbActionBase
    {
        public ShowPerformanceCriticalOutgoingCallsBulbAction(IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => ShowPerformanceCriticalCallsUtil.OUTGOING_MESSAGE;
        protected override ShowCallsType CallsType => ShowCallsType.OUTGOING;
    }
}