using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceOrExpensiveContextActionBase : PerformanceAnalysisContextActionBase
    {
        protected PerformanceOrExpensiveContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        
        protected sealed override bool ShouldCreate(IMethodDeclaration methodDeclaration)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            var isExpensiveContext = ExpensiveContextProvider.IsMarkedSweaDependent(declaredElement, SolutionAnalysisService);

            return isExpensiveContext || PerformanceContextProvider.IsMarkedSweaDependent(declaredElement, SolutionAnalysisService);
        }
    }
}