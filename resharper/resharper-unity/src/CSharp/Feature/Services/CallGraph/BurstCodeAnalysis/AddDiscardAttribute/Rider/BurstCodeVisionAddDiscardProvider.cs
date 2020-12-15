using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute.Rider
{
    [SolutionComponent]
    public class BurstCodeVisionAddDiscardProvider : IBurstCodeVisionMenuItemProvider
    {
        private readonly ISolution mySolution;

        public BurstCodeVisionAddDiscardProvider(ISolution solution)
        {
            mySolution = solution;
        }

        public BulbMenuItem GetMenuItem(IMethodDeclaration methodDeclaration, ITextControl textControl)
        {
            var bulb = new AddDiscardAttributeBulbAction(methodDeclaration);
            var item = UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, mySolution, BulbThemedIcons.ContextAction.Id);

            return item;
        }
    }
}