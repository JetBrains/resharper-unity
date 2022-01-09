using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Psi
{
    [LanguageDefinition(Name)]
    public class CgLanguage : KnownLanguage
    {
        public new const string Name = "CG";

        [CanBeNull, UsedImplicitly]
        public static CgLanguage Instance { get; private set; }

        public CgLanguage() : base(Name, "Cg")
        {
        }
    }
}
