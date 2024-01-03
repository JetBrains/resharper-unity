using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    public class ShowPerformanceCriticalCallsBulbAction : ShowMethodCallsBulbActionBase
    {
        public ShowPerformanceCriticalCallsBulbAction(IMethodDeclaration methodDeclaration, ShowCallsType callsType)
            : base(methodDeclaration, callsType)
        {
        }

        public override string Text
        {
            get
            {
                switch (CallsType)
                {
                    case ShowCallsType.INCOMING:
                        return Strings.ShowPerformanceCriticalCallsBulbAction_Text_Show_incoming_Performance_Critical_calls;
                    case ShowCallsType.OUTGOING:
                        return Strings.ShowPerformanceCriticalCallsBulbAction_Text_Show_outgoing_Performance_Critical_calls;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(CallsType), CallsType, null);
                }
            }
        }

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