using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.Rider;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute.Rider
{
    [SolutionComponent]
    public class AddDiscardCodeVisionProvider : SimpleCodeVisionMenuItemProviderBase
    {
        public AddDiscardCodeVisionProvider(ISolution solution)
            : base(solution)
        {
        }

        protected override IBulbAction GetAction(IMethodDeclaration methodDeclaration)
        {
            return new AddDiscardAttributeBulbAction(methodDeclaration);
        }
    }
}