using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.
    PerformanceAnalysis
{
    public abstract class ExpensiveOnlyContextActionBase : CallGraphContextActionBase
    {
        private readonly PerformanceCriticalContextProvider myPerformanceContextProvider;
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        protected ExpensiveOnlyContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
            myPerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
        }

        protected sealed override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration containingMethod)
        {
            return PerformanceAnalysisUtil.IsAvailable(containingMethod);
        }

        protected override bool ShouldCreate(IMethodDeclaration containingMethod)
        {
            var isExpensiveContext = myExpensiveContextProvider.IsMarkedSwea(containingMethod);
            var isPerformanceContext = myPerformanceContextProvider.IsMarkedSwea(containingMethod);

            return isPerformanceContext && !isExpensiveContext;
        }
    }
}