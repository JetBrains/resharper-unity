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
    // Note that there isn't a default searcher factory for YAML because it has no declared elements to search for. This
    // searcher factory is looking for C# declared elements - class or method, etc.
    [PsiSharedComponent]
    public class UnityYamlUsageSearchFactory : DomainSpecificSearcherFactoryBase
    {
        public override bool IsCompatibleWithLanguage(PsiLanguageType languageType)
        {
            return languageType.Is<YamlLanguage>();
        }

        public override IDomainSpecificSearcher CreateReferenceSearcher(IDeclaredElementsSet elements,
            bool findCandidates)
        {
            // We're interested in classes for usages of MonoScript references, and methods for UnityEvent usages
            if (elements.All(e => e is IClass || e is IMethod))
                return new YamlReferenceSearcher(this, elements, findCandidates);
            return null;
        }

        // Used to filter files before searching for reference. Method references require the element short name while
        // class references use the class's file's asset guid. If the file doesn't contain one of these words, it won't
        // be searched
        public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
        {
            var metaFileGuidCache = element.GetSolution().GetComponent<MetaFileGuidCache>();
            var words = new FrugalLocalList<string>();
            words.Add(element.ShortName);
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
