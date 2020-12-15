using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceExpensiveContextActionBase : CallGraphContextActionBase
    {
        private readonly PerformanceCriticalContextProvider myPerformanceContextProvider;
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        protected PerformanceExpensiveContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
            myPerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
        }

        protected sealed override bool ShouldCreate(IMethodDeclaration methodDeclaration)
        {
            var isExpensiveContext = myExpensiveContextProvider.IsMarkedSwea(methodDeclaration);

            return isExpensiveContext || myPerformanceContextProvider.IsMarkedSwea(methodDeclaration);
        }

        protected sealed override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration containingMethod)
        {
            return PerformanceAnalysisUtil.IsAvailable(containingMethod);
        }
    }
}