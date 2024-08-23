using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.RdBackend.Common.Features.ProjectModel.View;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Documents.View
{
    [ShellComponent(Instantiation.DemandAnyThreadUnsafe)]
    public class UnityAndroidDetector: ProjectModelViewPresenterExtension
    {
        public override bool TryAddUserData(IProjectMark projectMark, IProject project, out string name, out string value)
        {
            if (project == null)
                return base.TryAddUserData(projectMark, null, out name, out value);

            foreach (var configuration in project.ProjectProperties.ActiveConfigurations.Configurations.OfType<IManagedProjectConfiguration>())
            {
                var defines = configuration.DefineConstants;
                if (defines.Contains("ANDROID") && defines.Contains("UNITY"))
                {
                    name = "RequiredAndroidPlugin";
                    value = "true";
                    return true;
                }
            }

            name = null;
            value = null;
            
            return base.TryAddUserData(projectMark, project, out name, out value);
        }
    }
}