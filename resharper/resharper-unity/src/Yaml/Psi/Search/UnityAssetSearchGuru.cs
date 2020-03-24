using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetMethods;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetUsages;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    [SearchGuru(SearchGuruPerformanceEnum.FastFilterOutByIndex)]
    public class UnityYamlSearchGuru : ISearchGuru
    {
        private readonly AssetUsagesElementContainer myAssetUsagesElementContainer;
        private readonly AssetMethodsElementContainer myAssetMethodsElementContainer;

        public UnityYamlSearchGuru(UnityApi unityApi, AssetUsagesElementContainer assetUsagesElementContainer, AssetMethodsElementContainer assetMethodsElementContainer)
        {
            myAssetUsagesElementContainer = assetUsagesElementContainer;
            myAssetMethodsElementContainer = assetMethodsElementContainer;
        }

        // Allows us to filter the words that are collected from IDomainSpecificSearchFactory.GetAllPossibleWordsInFile
        // This is an ANY search
        public IEnumerable<string> BuzzWordFilter(IDeclaredElement searchFor, IEnumerable<string> containsWords) =>
            containsWords;

        public bool IsAvailable(SearchPattern pattern) => (pattern & SearchPattern.FIND_USAGES) != 0;

        // Return a context object for the item being searched for, or null if the element isn't interesting.
        // CanContainReferences isn't called if we return null. Do the work once here, then use it multiple times for
        // each file in CanContainReferences
        public object GetElementId(IDeclaredElement element)
        {
            if (!UnityYamlUsageSearchFactory.IsInterestingElement(element))
                return null;

            var set = new JetHashSet<IPsiSourceFile>();
            switch (element)
            {
                case IClass _class:
                    foreach (var sourceFile in myAssetUsagesElementContainer.GetPossibleFilesWithUsage(_class))
                        set.Add(sourceFile);
                    break;
                case IProperty _:
                case IMethod _:
                    foreach (var sourceFile in myAssetMethodsElementContainer.GetPossibleFilesWithUsage(element))
                        set.Add(sourceFile);
                    break;
            }
            
            return new UnityYamlSearchGuruId(set);
        }

        // False means definitely not, true means "maybe"
        public bool CanContainReferences(IPsiSourceFile sourceFile, object elementId)
        {
            // Meta files never contain references
            if (sourceFile.IsMeta())
                return false;

            if (sourceFile.IsAsset())
            {
                // We know the file matches ANY of the search terms, see if it also matches ALL of the search terms
                return ((UnityYamlSearchGuruId) elementId).Files.Contains(sourceFile);
            }

            // Not a YAML file, don't exclude it
            return true;
        }

        private class UnityYamlSearchGuruId
        {
            public JetHashSet<IPsiSourceFile> Files { get; }

            public UnityYamlSearchGuruId(JetHashSet<IPsiSourceFile> files)
            {
                Files = files;
            }
        }
    }
}