using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.Psi.Cg;

namespace JetBrains.ReSharper.Plugins.Unity.Daemon.Cg.Stages
{
    [SolutionComponent]
    public class CgIdentifierTooltipProvider : IdentifierTooltipProvider<CgLanguage>
    {
        public CgIdentifierTooltipProvider(Lifetime lifetime, ISolution solution, IDeclaredElementDescriptionPresenter presenter)
            : base(lifetime, solution, presenter)
        {
        }
    }
}