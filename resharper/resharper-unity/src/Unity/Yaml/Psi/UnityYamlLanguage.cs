using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [LanguageDefinition(Name)]
    public class UnityYamlLanguage : KnownLanguage
    {
        public new const string Name = "UnityYaml";

        [CanBeNull, UsedImplicitly]
        public static UnityYamlLanguage Instance { get; private set; }

        public UnityYamlLanguage() : base(Name, "Unity Yaml")
        {
        }
    }
}
