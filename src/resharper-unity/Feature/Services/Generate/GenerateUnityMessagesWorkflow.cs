using JetBrains.Application.DataContext;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Feature.Services.Generate
{
    public class GenerateUnityMessagesWorkflow : GenerateCodeWorkflowBase
    {
        public GenerateUnityMessagesWorkflow()
            : base(
                GeneratorUnityKinds.UnityMessages, LogoThemedIcons.UnityLogo.Id, "Unity3D Messages", GenerateActionGroup.CLR_LANGUAGE,
                "Unity3D Messages", "", "Generate.UnityMessage")
        {
        }

        public override double Order => 100;

        // Hides the menu item if it's not a Unity project
        public override bool IsAvailable(IDataContext dataContext)
        {
            var project = dataContext.GetData(ProjectModelDataConstants.PROJECT);
            if (project == null || !project.HasFlavour<UnityProjectFlavor>())
                return false;
            return base.IsAvailable(dataContext);
        }
    }
}