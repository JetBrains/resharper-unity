using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing
{
    internal interface IShaderLabParser : IParser
    {
        IVectorLiteral ParseVectorLiteral();
    }
}