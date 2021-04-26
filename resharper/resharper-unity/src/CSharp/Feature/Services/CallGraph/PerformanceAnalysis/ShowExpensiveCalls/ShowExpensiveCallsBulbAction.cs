using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls
{
    public class ShowExpensiveCallsBulbAction : ShowMethodCallsBulbActionBase
    {
        public ShowExpensiveCallsBulbAction(IMethodDeclaration methodDeclaration, ShowCallsType callsType)
            : base(methodDeclaration, callsType)
        {
        }

        public override string Text => ShowExpensiveCallsUtil.GetExpensiveShowCallsText(CallsType);

        protected sealed override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
        {
            var expensiveContextProvider = solution.GetComponent<ExpensiveInvocationContextProvider>();
            
            return CallGraphActionUtil.GetSimpleFilter(solution, expensiveContextProvider, CallsType);
        }

        public static IEnumerable<ShowExpensiveCallsBulbAction> GetExpensiveCallsActions(IMethodDeclaration methodDeclaration)
        {
            // var incoming = new ShowExpensiveCallsBulbAction(methodDeclaration, ShowCallsType.INCOMING);
            var outgoing = new ShowExpensiveCallsBulbAction(methodDeclaration, ShowCallsType.OUTGOING);

            return new[]
            {
                // incoming,
                outgoing
            };
        }
    }
}