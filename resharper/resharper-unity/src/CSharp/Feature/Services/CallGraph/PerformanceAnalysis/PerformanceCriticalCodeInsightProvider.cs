using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceCriticalCodeInsightProvider : SimpleCodeInsightMenuItemProviderBase, IPerformanceAnalysisCodeInsightMenuItemProvider
    {
        private readonly PerformanceCriticalContextProvider myPerformanceCriticalContextProvider;

        protected PerformanceCriticalCodeInsightProvider(ISolution solution)
            : base(solution)
        {
            myPerformanceCriticalContextProvider = solution.GetComponent<PerformanceCriticalContextProvider>();
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            
            return myPerformanceCriticalContextProvider.IsMarkedStage(declaredElement, context);
        }
    }
}