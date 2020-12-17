using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls
{
    public sealed class ShowBurstCallsBulbAction : ShowMethodCallsBulbActionBase
    {
        public ShowBurstCallsBulbAction([NotNull] IMethodDeclaration methodDeclaration, ShowCallsType type)
            : base(methodDeclaration, type)
        {
        }

        public override string Text => ShowBurstCallsUtil.GetBurstShowCallsText(CallsType);

        protected override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
        {
            var burstContextProvider = solution.GetComponent<BurstContextProvider>();

            return result =>
            {
                solution.Locks.AssertReadAccessAllowed();
                
                var referenceElement = result.ReferenceElement;
                var containing = (referenceElement as ICSharpTreeNode)?.GetContainingFunctionLikeDeclarationOrClosure();

                return burstContextProvider.IsMarkedSync(containing);
            };
        }

        public static IEnumerable<ShowBurstCallsBulbAction> GetAllCalls(IMethodDeclaration methodDeclaration)
        {
            var incoming = new ShowBurstCallsBulbAction(methodDeclaration, ShowCallsType.INCOMING);
            var outgoing = new ShowBurstCallsBulbAction(methodDeclaration, ShowCallsType.OUTGOING);

            return new[] {incoming, outgoing};
        }
    }
}