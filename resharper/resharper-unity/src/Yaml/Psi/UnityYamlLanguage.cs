using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    [LanguageDefinition(Name)]
    public class UnityYamlLanguage : YamlLanguage
    {
        public new const string Name = "UnityYaml";

        [CanBeNull, UsedImplicitly]
        public new static UnityYamlLanguage Instance { get; private set; }

        public UnityYamlLanguage() : base(Name, "Unity Yaml")
        {
        }
    }
}
