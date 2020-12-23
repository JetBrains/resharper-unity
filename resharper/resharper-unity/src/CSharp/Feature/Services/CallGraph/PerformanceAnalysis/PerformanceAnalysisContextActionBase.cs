using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceAnalysisContextActionBase: CallGraphContextActionBase
    {
        [NotNull] protected readonly PerformanceCriticalContextProvider PerformanceContextProvider;
        [NotNull] protected readonly ExpensiveInvocationContextProvider ExpensiveContextProvider;
        [NotNull] protected readonly SolutionAnalysisService SolutionAnalysisService;

        protected PerformanceAnalysisContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            ExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>().NotNull();
            PerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>().NotNull();
            SolutionAnalysisService = dataProvider.Solution.GetComponent<SolutionAnalysisService>().NotNull();
        }

        protected sealed override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration containingMethod)
        {
            return PerformanceAnalysisUtil.IsAvailable(containingMethod);
        }
    }
}