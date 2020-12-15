using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.Rider;
using JetBrains.ReSharper.Plugins.Unity.Rider.Highlightings.IconsProviders;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment.Rider
{
    [SolutionComponent]
    public sealed class PerformanceAnalysisCodeVisionAddDisableCommentProvider : SimpleCodeVisionMenuItemProviderBase, IPerformanceAnalysisCodeVisionMenuItemProvider
    {

        public PerformanceAnalysisCodeVisionAddDisableCommentProvider(ISolution solution) : base(solution)
        {
        }
        
        protected override IBulbAction GetAction(IMethodDeclaration methodDeclaration)
        {
            return new AddPerformanceAnalysisDisableCommentBulbAction(methodDeclaration);
        }
    }
}