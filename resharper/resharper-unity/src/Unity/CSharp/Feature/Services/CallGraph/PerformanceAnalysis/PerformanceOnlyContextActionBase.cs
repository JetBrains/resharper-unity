using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceOnlyContextActionBase : PerformanceAnalysisContextActionBase
    {
        protected PerformanceOnlyContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override bool ShouldCreate(IMethodDeclaration containingMethod)
        {
            var declaredElement = containingMethod.DeclaredElement;
            var isExpensiveContext = ExpensiveContextProvider.IsMarkedSweaDependent(declaredElement, SolutionAnalysisService);
            var isPerformanceContext = PerformanceContextProvider.IsMarkedSweaDependent(declaredElement, SolutionAnalysisService);

            return isPerformanceContext && !isExpensiveContext;
        }
    }
}