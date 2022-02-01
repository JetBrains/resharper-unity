using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi
{
    [LanguageDefinition(Name)]
    public class ShaderLabLanguage : KnownLanguage
    {
        public new const string Name = "SHADERLAB";

        [CanBeNull, UsedImplicitly]
        public static ShaderLabLanguage Instance { get; private set; }

        public ShaderLabLanguage()
            : base(Name, "ShaderLab")
        {
        }
    }
}
