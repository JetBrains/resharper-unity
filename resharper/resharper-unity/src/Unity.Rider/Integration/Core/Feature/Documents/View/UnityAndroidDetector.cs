using System.Linq;
using JetBrains.Application;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.RdBackend.Common.Env;
using JetBrains.RdBackend.Common.Features.ProjectModel.View;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.Documents.View
{
    [ShellComponent]
    public class UnityAndroidDetector: ProjectModelViewPresenterExtension
    {
        public override bool TryAddUserData(IProjectFile projectFile, out string name, out string value)
        {
            var project = projectFile.GetProject();
            if (project == null)
                return base.TryAddUserData(projectFile, out name, out value);

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
            return base.TryAddUserData(projectFile, out name, out value);
        }
    }
}