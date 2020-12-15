using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls.Outgoing
{
    public sealed class ShowBurstOutgoingCallsBulbAction : ShowBurstCallsBulbActionBase
    {
        public ShowBurstOutgoingCallsBulbAction([NotNull] IMethodDeclaration method) : base(method)
        {
        }
        public override string Text => ShowBurstCallsUtil.OUTGOING_MESSAGE;
        protected override ShowCallsType CallsType => ShowCallsType.OUTGOING;
    }
}