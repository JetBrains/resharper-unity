using JetBrains.Application.DataContext;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Generate.Dots
{
    public class GenerateRefFieldsAccessorsWorkflow : GenerateCodeWorkflowBase
    {
        public GenerateRefFieldsAccessorsWorkflow()
            : base(GeneratorUnityKinds.UnityGenerateRefAccessors, LogoIcons.Unity.Id,
                Strings.UnityDots_GenerateRefAccessors_Unity_Component_Fields_Title, GenerateActionGroup.CLR_LANGUAGE,
                Strings.UnityDots_GenerateRefAccessors_Unity_Component_Fields_WindowTitle,
                Strings.UnityDots_GenerateRefAccessors_Unity_Component_Fields_Description, "Generate.RefAccessors")
        {
        }

        public override double Order => 100;

        public override bool IsAvailable(IDataContext dataContext)
        {
            var project = dataContext.GetData(ProjectModelDataConstants.PROJECT);
            if (project == null || !project.IsUnityProject())
                return false;
            
            return base.IsAvailable(dataContext);
        }
    }
}