using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.PerformanceAnalysisDisableByComment;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceAnalysisSimpleMethodContextActionBase : SimpleMethodSingleContextActionBase, IContextAction
    {
        [NotNull] protected readonly PerformanceCriticalContextProvider PerformanceContextProvider;
        [NotNull] protected readonly ExpensiveInvocationContextProvider ExpensiveContextProvider;

        protected PerformanceAnalysisSimpleMethodContextActionBase(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            PerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
            ExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var methodDeclaration = CurrentMethodDeclaration;
            
            return PerformanceDisableUtil.IsAvailable(methodDeclaration);
        }
    }
}