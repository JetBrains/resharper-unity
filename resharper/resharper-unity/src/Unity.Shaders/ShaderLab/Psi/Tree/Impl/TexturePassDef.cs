#nullable enable
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class TexturePassDef
    {
        public override ITokenNode? GetEntityNameToken() => (Value as ITexturePassValue)?.StateCommandsEnumerable.LastOrDefaultOfType<IStateCommand, INameCommand>()?.Name;
    }
}