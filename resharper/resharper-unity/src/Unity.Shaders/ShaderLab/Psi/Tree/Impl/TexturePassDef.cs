#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.DeclaredElements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class TexturePassDef
    {    
        protected override DeclaredElementType DeclaredElementType => ShaderLabDeclaredElementType.TexturePassDef;

        public override ITokenNode? GetEntityNameToken() => (Value as ITexturePassValue)?.StateCommandsEnumerable.LastOrDefaultOfType<IStateCommand, INameCommand>()?.Name;
    }
}