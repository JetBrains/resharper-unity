using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
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
    [PsiComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityYamlSearchFilter : ISearchFilter
    {
        [NotNull, ItemNotNull] private readonly IEnumerable<IScriptUsagesElementContainer> myScriptsUsagesElementContainers;
        private readonly ILazy<UnityEventsElementContainer> myUnityEventsElementContainer;
        private readonly ILazy<AnimExplicitUsagesContainer> myAnimExplicitUsagesContainer;
        private readonly ILazy<AnimImplicitUsagesContainer> myAnimImplicitUsagesContainer;
        private readonly ILazy<AssetInspectorValuesContainer> myInspectorValuesContainer;

        public UnityYamlSearchFilter(UnityApi unityApi,
                                   [NotNull, ItemNotNull] IEnumerable<IScriptUsagesElementContainer> scriptsUsagesElementContainers,
                                   ILazy<UnityEventsElementContainer> unityEventsElementContainer,
                                   ILazy<AnimExplicitUsagesContainer> animExplicitUsagesContainer,
                                   ILazy<AnimImplicitUsagesContainer> animImplicitUsagesContainer,
                                   ILazy<AssetInspectorValuesContainer> container)
        {
            myScriptsUsagesElementContainers = scriptsUsagesElementContainers;
            myUnityEventsElementContainer = unityEventsElementContainer;
            myAnimExplicitUsagesContainer = animExplicitUsagesContainer;
            myAnimImplicitUsagesContainer = animImplicitUsagesContainer;
            myInspectorValuesContainer = container;
        }

        public SearchFilterKind Kind => SearchFilterKind.Cache;

        public bool IsAvailable(SearchPattern pattern) => (pattern & SearchPattern.FIND_USAGES) != 0;

        // Return a context object for the item being searched for, or null if the element isn't interesting.
        // CanContainReferences isn't called if we return null. Do the work once here, then use it multiple times for
        // each file in CanContainReferences
        public object TryGetKey(IDeclaredElement element)
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
                    CollectSourceFilesWithUsage(element as ITypeOwner, element, set);
                    foreach (var file in myAnimExplicitUsagesContainer.Value.GetPossibleFilesWithUsage(element))
                        set.Add(file);
                    foreach (var file in myAnimImplicitUsagesContainer.Value.GetPossibleFilesWithUsage(element))
                        set.Add(file);
                    foreach (var sourceFile in myUnityEventsElementContainer.Value.GetPossibleFilesWithUsage(element))
                        set.Add(sourceFile);
                    break;
                case IField field:
                    CollectSourceFilesWithUsage(field, element, set);
                    break;
            }

            return new UnityYamlSearchFilterKey(set);
        }

        private void CollectSourceFilesWithUsage([CanBeNull] ITypeOwner field, IDeclaredElement element, JetHashSet<IPsiSourceFile> set)
        {
            if (field == null)
                return;
                
            if (field.Type.GetTypeElement().DerivesFromUnityEvent())
            {
                foreach (var sourceFile in myUnityEventsElementContainer.Value.GetPossibleFilesWithUsage(element))
                    set.Add(sourceFile);
            }
            else
            {
                foreach (var sourceFile in myInspectorValuesContainer.Value.GetPossibleFilesWithUsage(field))
                    set.Add(sourceFile);
            }
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
                return ((UnityYamlSearchFilterKey) elementId).Files.Contains(sourceFile);
            }

            // Not a YAML file, don't exclude it
            return true;
        }

        private class UnityYamlSearchFilterKey
        {
            public JetHashSet<IPsiSourceFile> Files { get; }

            public UnityYamlSearchFilterKey(JetHashSet<IPsiSourceFile> files)
            {
                Files = files;
            }
        }
    }
}