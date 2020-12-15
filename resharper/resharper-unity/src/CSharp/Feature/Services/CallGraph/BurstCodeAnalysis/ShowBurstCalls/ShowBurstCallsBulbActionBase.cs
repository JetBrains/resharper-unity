using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls
{
    public abstract class ShowBurstCallsBulbActionBase : ShowMethodCallsBulbActionBase
    {
        protected ShowBurstCallsBulbActionBase([NotNull] IMethodDeclaration method)
            : base(method)
        {
        }
        
        protected override Func<CallHierarchyFindResult, bool> GetFilter(ISolution solution)
        {
            var burstContextProvider = solution.GetComponent<BurstContextProvider>();

            return result =>
            {
                var referenceElement = result.ReferenceElement;
                var containing = (referenceElement as ICSharpTreeNode)?.GetContainingFunctionLikeDeclarationOrClosure();

                // CGTD filter
                
                return burstContextProvider.IsMarkedSync(containing);
            };
        }
    }
}