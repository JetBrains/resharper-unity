#nullable enable
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class GrabPassDef
    {
        protected override string TryGetDeclaredName()
        {
            var nameToken = (Value as IGrabPassValue)?.CommandsEnumerable.LastOrDefault()?.Name;
            return ShaderLabTreeHelpers.FormatCommandDeclaredName(ShaderLabTokenType.GRABPASS_KEYWORD, nameToken);
        }
    }
}