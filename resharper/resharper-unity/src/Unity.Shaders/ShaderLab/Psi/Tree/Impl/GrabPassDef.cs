#nullable enable
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree.Impl
{
    internal partial class GrabPassDef
    {
        public override ITokenNode? GetEntityNameToken() => (Value as IGrabPassValue)?.CommandsEnumerable.LastOrDefault()?.Name;
    }
}