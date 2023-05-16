#nullable enable

using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    public abstract class ShaderLabIncludeBase : ShaderLabCodeBlockBase
    {
        protected sealed override DeclaredElementType ElementType => ShaderLabDeclaredElementType.IncludeBlock;
    }
}