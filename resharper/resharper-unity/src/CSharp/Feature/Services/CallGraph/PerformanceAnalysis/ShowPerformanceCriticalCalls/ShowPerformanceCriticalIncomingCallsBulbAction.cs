using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    public class ShowPerformanceCriticalIncomingCallsBulbAction : ShowMethodCallsBulbActionBase
    {
        public ShowPerformanceCriticalIncomingCallsBulbAction(IMethodDeclaration methodDeclaration, ShowCallsType callsType)
            : base(methodDeclaration, callsType)
        {
        }

        public override string Text => ShowPerformanceCriticalCallsUtil.GetPerformanceCriticalShowCallsText(CallsType);

        protected override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
        {
            var performanceCriticalContextProvider = solution.GetComponent<PerformanceCriticalContextProvider>();

            return result =>
            {
                var referenceElement = result.ReferenceElement;
                var containing = (referenceElement as ICSharpTreeNode)?.GetContainingFunctionLikeDeclarationOrClosure();

                // CGTD filter reference
                return performanceCriticalContextProvider.IsMarkedSync(containing);
            };
        }

        public static IEnumerable<ShowPerformanceCriticalIncomingCallsBulbAction> GetAllCalls(IMethodDeclaration methodDeclaration)
        {
            var incoming = new ShowPerformanceCriticalIncomingCallsBulbAction(methodDeclaration, ShowCallsType.INCOMING);
            var outgoing = new ShowPerformanceCriticalIncomingCallsBulbAction(methodDeclaration, ShowCallsType.OUTGOING);

            return new[] {incoming, outgoing};
        }
    }
}