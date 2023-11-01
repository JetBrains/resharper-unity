#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;

public interface IShaderPlatformInfoProvider
{
    ShaderApi ShaderApi { get; }
}