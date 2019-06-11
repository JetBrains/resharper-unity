using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.UnityYaml.Psi
{
    [LanguageDefinition(Name)]
    public class UnityYamlLanguage : KnownLanguage
    {
        public new const string Name = "UnityYaml";

        [CanBeNull] public static readonly UnityYamlLanguage Instance = null;

        public UnityYamlLanguage()
            : base(Name, "UnityYaml")
        {
        }
    }
}