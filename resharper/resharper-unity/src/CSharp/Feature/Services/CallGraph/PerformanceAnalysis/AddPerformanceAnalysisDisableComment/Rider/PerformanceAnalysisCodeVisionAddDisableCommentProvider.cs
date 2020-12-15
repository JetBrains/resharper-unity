using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment.Rider
{
    [SolutionComponent]
    public sealed class PerformanceAnalysisCodeVisionAddDisableCommentProvider : IPerformanceAnalysisCodeVisionMenuItemProvider
    {
        private readonly ISolution mySolution;

        public PerformanceAnalysisCodeVisionAddDisableCommentProvider(ISolution solution)
        {
            mySolution = solution;
        }
        
        public BulbMenuItem GetMenuItem(IMethodDeclaration methodDeclaration, ITextControl textControl, DaemonProcessKind processKind)
        {
            var bulb = new AddPerformanceAnalysisDisableCommentBulbAction(methodDeclaration);
            var item = UnityCallGraphUtil.BulbActionToMenuItem(bulb, textControl, mySolution, BulbThemedIcons.ContextAction.Id);

            return item;
        }
    }
}