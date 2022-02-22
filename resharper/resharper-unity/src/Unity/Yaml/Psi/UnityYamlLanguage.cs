using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi
{
    // Unity YAML files get their own PSI language, which gives us better control of handling possibly massive files.
    // For example, the default lexer will not tokenize YAML documents we're not interested in. We have a UnityYaml PSI
    // language, ProjectFileType, ProjectFileLanguageService, lexer, etc. but no PSI language service, and therefore no
    // infrastructure to create a parser. We only parse these potentially massive files on demand, when indexed
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
