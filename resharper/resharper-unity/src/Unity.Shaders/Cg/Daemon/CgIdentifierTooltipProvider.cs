using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.UI;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Daemon
{
    [SolutionComponent]
    public class CgIdentifierTooltipProvider : IdentifierTooltipProvider<CgLanguage>
    {
        public CgIdentifierTooltipProvider(Lifetime lifetime, ISolution solution, IDeclaredElementDescriptionPresenter presenter, DeclaredElementPresenterTextStylesService declaredElementPresenterTextStylesService)
            :  base(lifetime, solution, presenter, declaredElementPresenterTextStylesService)
        {
        }
    }
}