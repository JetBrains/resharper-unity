using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    [PsiSharedComponent]
    public class UnityYamlGuidUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<YamlLanguage>();
        }

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements, bool findCandidates)
        {
            // We only use guids to find classes in YAML files
            if (elements.All(e => e is IClass))
                return new YamlReferenceSearcher(elements, findCandidates);
            return null;
        }

        // Used to filter files before searching for reference
        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            // The existing YAML domain searcher word list will find all files that contain the element's short name,
            // but won't find files that just have an asset guid
            var metaFileGuidCache = element.GetSolution().GetComponent<MetaFileGuidCache>();
            var words = new FrugalLocalList<string>();
            foreach (var sourceFile in element.GetSourceFiles())
            {
                var guid = metaFileGuidCache.GetAssetGuid(sourceFile);
                if (guid != null)
                    words.Add(guid);
            }

            return words.ToList();
        }
    }
}