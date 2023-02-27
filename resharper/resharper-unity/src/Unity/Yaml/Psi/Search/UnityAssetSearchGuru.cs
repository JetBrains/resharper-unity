using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Explicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.Anim.Implicit;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.UnityEvents;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Search
{
    [SearchGuru(SearchGuruPerformanceEnum.FastFilterOutByIndex)]
    public class UnityYamlSearchGuru : ISearchGuru
    {
        [NotNull, ItemNotNull] private readonly IEnumerable<IScriptUsagesElementContainer> myScriptsUsagesElementContainers;
        private readonly UnityEventsElementContainer myUnityEventsElementContainer;
        private readonly AnimExplicitUsagesContainer myAnimExplicitUsagesContainer;
        private readonly AnimImplicitUsagesContainer myAnimImplicitUsagesContainer;
        private readonly AssetInspectorValuesContainer myInspectorValuesContainer;

        public UnityYamlSearchGuru(UnityApi unityApi,
                                   [NotNull, ItemNotNull] IEnumerable<IScriptUsagesElementContainer> scriptsUsagesElementContainers,
                                   UnityEventsElementContainer unityEventsElementContainer,
                                   AnimExplicitUsagesContainer animExplicitUsagesContainer,
                                   AnimImplicitUsagesContainer animImplicitUsagesContainer,
                                   AssetInspectorValuesContainer container)
        {
            myScriptsUsagesElementContainers = scriptsUsagesElementContainers;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myAnimExplicitUsagesContainer = animExplicitUsagesContainer;
            myAnimImplicitUsagesContainer = animImplicitUsagesContainer;
            myInspectorValuesContainer = container;
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
                case IClass scriptClass:
                    AddFilesWithPossibleScriptUsages(scriptClass, set);
                    break;
                case IProperty _:
                case IMethod _:
                    foreach (var file in myAnimExplicitUsagesContainer.GetPossibleFilesWithUsage(element))
                        set.Add(file);
                    foreach (var file in myAnimImplicitUsagesContainer.GetPossibleFilesWithUsage(element))
                        set.Add(file);
                    foreach (var sourceFile in myUnityEventsElementContainer.GetPossibleFilesWithUsage(element))
                        set.Add(sourceFile);
                    break;
                case IField field:
                    if (field.Type.GetTypeElement().DerivesFromUnityEvent())
                    {
                        foreach (var sourceFile in myUnityEventsElementContainer.GetPossibleFilesWithUsage(element))
                            set.Add(sourceFile);
                    }
                    else
                    {
                        foreach (var sourceFile in myInspectorValuesContainer.GetPossibleFilesWithUsage(field))
                            set.Add(sourceFile);
                    }

                    break;
            }

            return new UnityYamlSearchGuruId(set);
        }

        private void AddFilesWithPossibleScriptUsages([NotNull] IClass scriptClass,
                                                      [NotNull, ItemNotNull] ISet<IPsiSourceFile> filesSet)
        {
            foreach (var scriptsUsagesElementContainer in myScriptsUsagesElementContainers)
            {
                foreach (var sourceFile in scriptsUsagesElementContainer.GetPossibleFilesWithScriptUsages(scriptClass))
                    filesSet.Add(sourceFile);
            }
        }

        // False means definitely not, true means "maybe"
        public bool CanContainReferences(IPsiSourceFile sourceFile, object elementId)
        {
            // Meta files never contain references
            if (sourceFile.IsMeta())
                return false;

            if (sourceFile.IsYamlDataFile())
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