using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Daemon
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