using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [PsiComponent]
    public class UnityEventHandlerReferenceCache : SimpleICache<JetHashSet<UnityEventHandlerCacheItem>>
    {
        private readonly ISolution mySolution;

        // We need to be able to check if a method is declared on a base type but used in a deriving type. We keep a map
        // of method/property setter short name to all the asset guids where it's used. These usages will always be the
        // most derived type. If we get all (inherited) members of each usage, we can match to see if a given method
        // (potentially declared on a base type) is being used as an event handler
        private readonly CompactOneToSetMap<string, string> myShortNameToAssetGuid =
            new CompactOneToSetMap<string, string>();

        public UnityEventHandlerReferenceCache(Lifetime lifetime, ISolution solution,
                                               IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, CreateMarshaller())
        {
            mySolution = solution;
        }

        // Version "1": List<string> "asset::shortname"
        public override string Version => "2";

        private static IUnsafeMarshaller<JetHashSet<UnityEventHandlerCacheItem>> CreateMarshaller()
        {
            return UnsafeMarshallers.GetCollectionMarshaller(UnityEventHandlerCacheItem.Marshaller,
                n => new JetHashSet<UnityEventHandlerCacheItem>(n));
        }

        public bool IsEventHandler([NotNull] IMethod declaredElement)
        {
            var sourceFiles = declaredElement.GetSourceFiles();

            // The methods and property setters that we are interested in will only have a single source file
            if (sourceFiles.Count != 1)
                return false;

            foreach (var assetGuid in myShortNameToAssetGuid[declaredElement.ShortName])
            {
                var invokedType = UnityObjectPsiUtil.GetTypeElementFromScriptAssetGuid(mySolution, assetGuid);
                if (invokedType != null)
                {
                    var members = invokedType.GetAllClassMembers(declaredElement.ShortName);
                    foreach (var member in members)
                    {
                        if (Equals(member.Element, declaredElement))
                            return true;
                    }
                }
            }

            return false;
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<YamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   sourceFile.IsAsset();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            if (file == null)
                return null;

            var cacheItems = new JetHashSet<UnityEventHandlerCacheItem>();
            var referenceProcessor = new ConditionalRecursiveReferenceProcessor(reference =>
            {
                var assetGuid = reference.GetScriptAssetGuid();
                if (assetGuid != null)
                    cacheItems.Add(new UnityEventHandlerCacheItem(assetGuid, reference.EventHandlerName));
            });

            foreach (var document in file.DocumentsEnumerable)
            {
                if (UnityEventTargetReferenceFactory.CanContainReference(document))
                    referenceProcessor.ProcessForResolve(document);
            }

            return cacheItems.Count > 0 ? cacheItems : null;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
            AddToLocalCache(builtPart as JetHashSet<UnityEventHandlerCacheItem> ??
                            JetHashSet<UnityEventHandlerCacheItem>.Empty);
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (_, cacheItems) in Map)
                AddToLocalCache(cacheItems);
        }

        private void AddToLocalCache(IEnumerable<UnityEventHandlerCacheItem> cacheItems)
        {
            foreach (var cacheItem in cacheItems)
                myShortNameToAssetGuid.Add(cacheItem.ReferenceShortName, cacheItem.AssetGuid);
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItems))
            {
                foreach (var cacheItem in cacheItems)
                    myShortNameToAssetGuid.Remove(cacheItem.ReferenceShortName, cacheItem.AssetGuid);
            }
        }

        private class ConditionalRecursiveReferenceProcessor : RecursiveReferenceProcessor<UnityEventTargetReference>
        {
            public ConditionalRecursiveReferenceProcessor(Action<UnityEventTargetReference> action)
                : base(action)
            {
            }

            public override void ProcessBeforeInterior(ITreeNode element)
            {
                if (UnityEventTargetReferenceFactory.CanHaveReference(element))
                    base.ProcessBeforeInterior(element);
            }
        }
    }
}
