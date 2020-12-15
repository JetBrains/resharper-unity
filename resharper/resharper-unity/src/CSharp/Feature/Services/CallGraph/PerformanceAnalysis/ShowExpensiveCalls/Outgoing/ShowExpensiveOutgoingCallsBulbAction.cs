using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls.Outgoing
{
    public class ShowExpensiveOutgoingCallsBulbAction: ShowExpensiveCallsBulbActionBase
    {
        public ShowExpensiveOutgoingCallsBulbAction(IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => ShowExpensiveCallsUtil.OUTGOING_MESSAGE;
        protected override ShowCallsType CallsType => ShowCallsType.OUTGOING;
    }
}