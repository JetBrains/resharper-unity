#nullable enable
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Resolve
{
    public class ShaderLabResolveErrorType : ResolveErrorType
    {
        public static readonly ResolveErrorType SHADERLAB_SHADER_REFERENCE_UNRESOLVED_WARNING = new ShaderLabResolveErrorType("SHADERLAB_SHADER_REFERENCE_UNRESOLVED_WARNING");
        private ShaderLabResolveErrorType(string name) : base(name)
        {
        }
    }
}