#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class TexturePassDef
    {
        protected override string TryGetDeclaredName()
        {
            var nameToken = (Value as ITexturePassValue)?.StateCommandsEnumerable.LastOrDefaultOfType<IStateCommand, INameCommand>()?.Name;
            return ShaderLabTreeHelpers.FormatCommandDeclaredName(ShaderLabTokenType.PASS_KEYWORD, nameToken);
        }
    }
}