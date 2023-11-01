using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent]
    public class TestShaderProgramInfoProvider : IShaderPlatformInfoProvider
    {
        public ShaderApi ShaderApi { get; set; } = ShaderApi.D3D11;
    }
}