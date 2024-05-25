using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute
{
    [SolutionComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class AddDiscardBulbItemsProvider : BurstBulbItemsProvider
    {
        public AddDiscardBulbItemsProvider(ISolution solution, BurstContextProvider burstContextProvider)
            : base(solution, burstContextProvider)
        {
        }


        protected override IEnumerable<BulbMenuItem> GetActions(IMethodDeclaration methodDeclaration, ITextControl textControl)
        {
            var bulb = new AddDiscardAttributeBulbAction(methodDeclaration);
            var bulbMenuItem = UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, Solution, BulbThemedIcons.ContextAction.Id);

            return new[] {bulbMenuItem};
        }
    }
}