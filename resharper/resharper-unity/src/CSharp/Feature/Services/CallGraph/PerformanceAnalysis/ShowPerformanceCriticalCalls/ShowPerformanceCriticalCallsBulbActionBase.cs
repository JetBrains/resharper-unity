using System;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CallHierarchy.FindResults;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    public abstract class ShowPerformanceCriticalCallsBulbActionBase : ShowCallsBulbActionBase
    {
        private readonly DeclaredElementInstance<IClrDeclaredElement> myMethod;
        protected ShowPerformanceCriticalCallsBulbActionBase(IMethodDeclaration methodDeclaration)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            Assertion.AssertNotNull(declaredElement, "declared is null, should be impossible");
            myMethod = new DeclaredElementInstance<IClrDeclaredElement>(declaredElement);
        }

        protected override DeclaredElementInstance<IClrDeclaredElement> GetStartElement() => myMethod;

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
    }
}