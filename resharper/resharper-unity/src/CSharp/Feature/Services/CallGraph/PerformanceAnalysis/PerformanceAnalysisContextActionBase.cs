using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceAnalysisContextActionBase: CallGraphContextActionBase
    {
        protected readonly PerformanceCriticalContextProvider PerformanceContextProvider;
        protected readonly ExpensiveInvocationContextProvider ExpensiveContextProvider;

        protected PerformanceAnalysisContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            ExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
            PerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
        }

        protected sealed override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration containingMethod)
        {
            return PerformanceAnalysisUtil.IsAvailable(containingMethod);
        }
    }
}