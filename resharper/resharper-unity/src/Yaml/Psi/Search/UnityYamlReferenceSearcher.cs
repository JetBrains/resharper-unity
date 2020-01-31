using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    public class UnityYamlReferenceSearcher : YamlReferenceSearcher
    {
        public UnityYamlReferenceSearcher(IDomainSpecificSearcherFactory searchWordsProvider, IDeclaredElementsSet elements, bool findCandidates)
            : base(searchWordsProvider, elements, findCandidates)
        {
            // ElementNames.Remove(YamlTrigramIndexBuilder.YAML_REFERENCE_IDENTIFIER);
        }
    }
}