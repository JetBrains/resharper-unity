#nullable enable
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport
{
    public interface ICgIncludeDirectoryProvider
    {
        VirtualFileSystemPath GetCgIncludeFolderPath();
    }
    
    [SolutionComponent]
    public class CgIncludeDirectoryProvider : ICgIncludeDirectoryProvider
    {
        private readonly IUnityVersion myUnityVersion;

        public CgIncludeDirectoryProvider(IUnityVersion unityVersion)
        {
            myUnityVersion = unityVersion;
        }

        public virtual VirtualFileSystemPath GetCgIncludeFolderPath()
        {
            var path = myUnityVersion.GetActualAppPathForSolution();
            if (path.IsEmpty)
                return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);

            var contentPath = UnityInstallationFinder.GetApplicationContentsPath(path);
            return contentPath.Combine("CGIncludes");
        }        
    }
}