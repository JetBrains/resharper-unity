#nullable enable
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport
{
    public interface ICgIncludeDirectoryProvider
    {
        VirtualFileSystemPath GetCgIncludeFolderPath();
    }
    
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class CgIncludeDirectoryProvider(IUnityVersion unityVersion) : ICgIncludeDirectoryProvider
    {
        private static readonly ILogger ourLogger = Logger.GetLogger<CgIncludeDirectoryProvider>();

        public virtual VirtualFileSystemPath GetCgIncludeFolderPath()
        {
            var path = unityVersion.GetActualAppPathForSolution();
            if (path.IsEmpty)
                return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);

            var contentPath = UnityInstallationFinder.GetApplicationContentsPath(path);
            var cgIncludePath = contentPath.Combine("CGIncludes");
            if (!cgIncludePath.ExistsDirectory)
                cgIncludePath = contentPath.Combine("Resources/CGIncludes");
            if (!cgIncludePath.ExistsDirectory && unityVersion.ActualVersionForSolution.Value.Major >= 6000)
                ourLogger.Error($"CGIncludes not found at known locations for Unity 6000.x. Contents path: {contentPath}");
            return cgIncludePath;
        }
    }
}