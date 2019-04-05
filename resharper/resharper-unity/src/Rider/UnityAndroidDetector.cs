using System.Linq;
using JetBrains.Application;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Host.Features.ProjectModel.View;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [ShellComponent]
    public class UnityAndroidDetector: ProjectModelViewPresenterExtension
    {
        public override bool TryAddUserData(IProject project, out string name, out string value)
        {
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
            return false;
        }
    }
}