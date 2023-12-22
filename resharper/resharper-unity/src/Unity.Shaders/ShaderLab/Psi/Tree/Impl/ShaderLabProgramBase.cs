#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabProgramBase : ShaderLabCodeBlockBase
    {
        protected sealed override DeclaredElementType DeclaredElementType => ShaderLabDeclaredElementType.ProgramBlock;
    }
}