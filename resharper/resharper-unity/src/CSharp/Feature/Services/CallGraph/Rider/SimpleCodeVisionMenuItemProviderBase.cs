using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.Rider
{
    public abstract class SimpleCodeVisionMenuItemProviderBase : ICallGraphCodeVisionMenuItemProvider
    {
        private readonly ISolution mySolution;

        protected SimpleCodeVisionMenuItemProviderBase(ISolution solution)
        {
            mySolution = solution;
        }

        public BulbMenuItem GetMenuItem(IMethodDeclaration methodDeclaration, ITextControl textControl, DaemonProcessKind processKind)
        {
            if (!CheckCallGraph(methodDeclaration, processKind))
                return null;
            
            var bulb = GetAction(methodDeclaration);
            var item = UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, mySolution, BulbThemedIcons.ContextAction.Id);

            return item;
        }

        protected virtual bool CheckCallGraph(IMethodDeclaration methodDeclaration, DaemonProcessKind processKind) => true;

        protected abstract IBulbAction GetAction(IMethodDeclaration methodDeclaration);
    }
}