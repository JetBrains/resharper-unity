using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Tree;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing
{
    internal interface IShaderLabParser : IParser
    {
        IVectorLiteral ParseVectorLiteral();
    }
}