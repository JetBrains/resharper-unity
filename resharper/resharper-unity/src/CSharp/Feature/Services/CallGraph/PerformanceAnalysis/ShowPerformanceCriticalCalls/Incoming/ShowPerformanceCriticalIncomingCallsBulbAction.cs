using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls.Incoming
{
    public class ShowPerformanceCriticalIncomingCallsBulbAction : ShowPerformanceCriticalCallsBulbActionBase
    {
        public ShowPerformanceCriticalIncomingCallsBulbAction(IMethodDeclaration methodDeclaration) : base(methodDeclaration)
        {
        }

        public override string Text => ShowPerformanceCriticalCallsUtil.INCOMING_MESSAGE;

        protected override ShowCallsType CallsType => ShowCallsType.INCOMING;
    }
}