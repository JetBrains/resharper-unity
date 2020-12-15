using System;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls
{
    public abstract class ShowExpensiveCallsBulbActionBase : ShowMethodCallsBulbActionBase
    {
        protected ShowExpensiveCallsBulbActionBase(IMethodDeclaration methodDeclaration)
            : base(methodDeclaration)
        {
        }

        protected sealed override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
        {
            var expensiveContextProvider = solution.GetComponent<ExpensiveInvocationContextProvider>();

            return result =>
            {
                var referenceElement = result.ReferenceElement;
                var containing = (referenceElement as ICSharpTreeNode)?.GetContainingFunctionLikeDeclarationOrClosure();

                // CGTD filter reference
                return expensiveContextProvider.IsMarkedSync(containing);
            };
        }
    }
}