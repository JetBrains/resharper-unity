using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class CgIncludeDirectoryProviderStub : CgIncludeDirectoryProvider
    {
        public VirtualFileSystemPath? CgIncludeFolderPathOverride { get; set; }

        public CgIncludeDirectoryProviderStub(IUnityVersion unityVersion) : base(unityVersion)
        {
        }

        public override VirtualFileSystemPath GetCgIncludeFolderPath() => CgIncludeFolderPathOverride ?? base.GetCgIncludeFolderPath();
    }
}