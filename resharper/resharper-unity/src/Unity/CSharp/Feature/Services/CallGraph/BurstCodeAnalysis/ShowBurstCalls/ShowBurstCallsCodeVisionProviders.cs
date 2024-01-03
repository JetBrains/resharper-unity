using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls
{
    [SolutionComponent]
    public class ShowBurstCallsBulbItemsProvider : BurstBulbItemsProvider
    {
        private readonly SolutionAnalysisConfiguration myConfiguration;

        public ShowBurstCallsBulbItemsProvider(ISolution solution, BurstContextProvider burstContextProvider, SolutionAnalysisConfiguration configuration)
            : base(solution, burstContextProvider)
        {
            myConfiguration = configuration;
        }

        protected override IEnumerable<BulbMenuItem> GetActions(IMethodDeclaration methodDeclaration, ITextControl textControl)
        {
            var actions = ShowBurstCallsBulbAction.GetBurstCallsActions(methodDeclaration);

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