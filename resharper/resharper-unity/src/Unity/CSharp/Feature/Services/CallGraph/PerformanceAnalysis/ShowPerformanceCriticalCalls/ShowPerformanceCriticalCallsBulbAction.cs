using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    public class ShowPerformanceCriticalCallsBulbAction : ShowMethodCallsBulbActionBase
    {
        public ShowPerformanceCriticalCallsBulbAction(IMethodDeclaration methodDeclaration, ShowCallsType callsType)
            : base(methodDeclaration, callsType)
        {
        }

        public override string Text => ShowPerformanceCriticalCallsUtil.GetPerformanceCriticalShowCallsText(CallsType);

        protected override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
        {
            var performanceCriticalContextProvider = solution.GetComponent<PerformanceCriticalContextProvider>();
            
            return CallGraphActionUtil.GetSimpleFilter(solution, performanceCriticalContextProvider, CallsType);
        }

        public static IEnumerable<ShowPerformanceCriticalCallsBulbAction> GetPerformanceCallsActions(IMethodDeclaration methodDeclaration)
        {
            var incoming = new ShowPerformanceCriticalCallsBulbAction(methodDeclaration, ShowCallsType.INCOMING);
            // var outgoing = new ShowPerformanceCriticalIncomingCallsBulbAction(methodDeclaration, ShowCallsType.OUTGOING);

            return new[] {
                incoming
                // , outgoing
            };
        }
    }
}