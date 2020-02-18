using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [LanguageDefinition(Name)]
    public class UnityYamlDummyLanguage : KnownLanguage
    {
        public new const string Name = "UnityYamlDummy";

        [CanBeNull, UsedImplicitly]
        public static UnityYamlDummyLanguage Instance { get; private set; }

        public UnityYamlDummyLanguage() : base(Name, "Unity Yaml Dummy")
        {
        }
    }
}
