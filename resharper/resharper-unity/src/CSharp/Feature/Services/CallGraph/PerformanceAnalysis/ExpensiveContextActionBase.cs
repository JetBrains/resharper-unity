using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.
    PerformanceAnalysis
{
    public abstract class ExpensiveContextActionBase : PerformanceAnalysisContextActionBase
    {
        protected ExpensiveContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
        
        protected override bool ShouldCreate(IMethodDeclaration containingMethod)
        {
            var declaredElement = containingMethod.DeclaredElement;
            var isExpensiveContext = ExpensiveContextProvider.IsMarkedSweaDependent(declaredElement, SolutionAnalysisService);

            return isExpensiveContext;
        }
    }
}