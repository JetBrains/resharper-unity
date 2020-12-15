using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls.Incoming
{
    public class ShowExpensiveIncomingCallsBulbAction : ShowExpensiveCallsBulbActionBase
    {
        public ShowExpensiveIncomingCallsBulbAction(IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => ShowExpensiveCallsUtil.INCOMING_MESSAGE;
        protected override ShowCallsType CallsType => ShowCallsType.INCOMING;
    }
}