using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dots;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    public class GenerateBakerAndAuthoringActionWorkflow : GenerateCodeWorkflowBase
    {
        public GenerateBakerAndAuthoringActionWorkflow()
            : base(GeneratorUnityKinds.UnityGenerateBakerAndAuthoring, LogoIcons.Unity.Id, Strings.UnityDots_GenerateBakerAndAuthoring_Unity_Component_Fields_Title, GenerateActionGroup.CLR_LANGUAGE,
                Strings.UnityDots_GenerateBakerAndAuthoring_Unity_Component_Fields_WindowTitle, Strings.UnityDots_GenerateBakerAndAuthoring_Unity_Component_Fields_Description, "Generate.BakerAndAuthoring")
        {
        }

        public override double Order => 100;

        // Hides the menu item if it's not a Unity project
        public override bool IsAvailable(IDataContext dataContext)
        {
            if (!DotsUtils.IsUnityProjectWithEntitiesPackage(dataContext)) 
                return false;
            
            return base.IsAvailable(dataContext);
        }
    }
}