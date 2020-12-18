using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
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
            var isExpensiveContext = ExpensiveContextProvider.IsMarkedSwea(methodDeclaration);

            return isExpensiveContext || PerformanceContextProvider.IsMarkedSwea(methodDeclaration);
        }
    }
}