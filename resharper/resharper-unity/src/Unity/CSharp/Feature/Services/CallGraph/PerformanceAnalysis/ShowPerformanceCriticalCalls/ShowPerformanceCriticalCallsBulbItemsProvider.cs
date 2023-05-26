using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    [SolutionComponent]
    public class ShowPerformanceCriticalCallsBulbItemsProvider : PerformanceCriticalBulbItemsProvider
    {
        private readonly SolutionAnalysisConfiguration myConfiguration;

        public ShowPerformanceCriticalCallsBulbItemsProvider(SolutionAnalysisConfiguration configuration, PerformanceCriticalContextProvider performanceCriticalContextProvider, ISolution solution) : base(solution, performanceCriticalContextProvider)
        {
            myConfiguration = configuration;
        }

        protected override IEnumerable<BulbMenuItem> GetActions(IMethodDeclaration methodDeclaration, ITextControl textControl)
        {
            var actions = ShowPerformanceCriticalCallsBulbAction.GetPerformanceCallsActions(methodDeclaration);

            return actions.ToMenuItems(textControl, Solution);
        }

        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            if (!UnityCallGraphUtil.IsCallGraphReady(myConfiguration))
                return false;
            
            return base.CheckCallGraph(methodDeclaration, context);
        }
    }
}