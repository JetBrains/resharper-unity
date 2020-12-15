using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls.Incoming
{
    public sealed class ShowBurstIncomingCallsBulbAction : ShowBurstCallsBulbActionBase
    {
        public ShowBurstIncomingCallsBulbAction([NotNull] IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        public override string Text => ShowBurstCallsUtil.INCOMING_MESSAGE;
        protected override ShowCallsType CallsType => ShowCallsType.INCOMING;
    }
}