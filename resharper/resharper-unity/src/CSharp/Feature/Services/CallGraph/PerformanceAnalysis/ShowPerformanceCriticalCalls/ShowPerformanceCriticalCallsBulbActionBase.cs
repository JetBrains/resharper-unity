using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    public abstract class ShowPerformanceCriticalCallsBulbActionBase : ShowMethodCallsBulbActionBase
    {
        protected ShowPerformanceCriticalCallsBulbActionBase(IMethodDeclaration methodDeclaration) : base(methodDeclaration)
        {
        }
        
        protected sealed override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
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
    }
}