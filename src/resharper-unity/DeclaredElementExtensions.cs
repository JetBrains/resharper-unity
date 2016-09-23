using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class DeclaredElementExtensions
    {
        public static bool IsFromUnityProject(this IDeclaredElement element)
        {
            return element.GetSourceFiles().Any(sf =>
            {
                var project = sf.GetProject();
                return project != null && project.HasFlavour<UnityProjectFlavor>();
            });
        }
    }
}