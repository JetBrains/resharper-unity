using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DataFlow;
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
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [PsiComponent]
    public class UnityEventHandlerReferenceCache : SimpleICache<List<string>>
    {
        private readonly MetaFileGuidCache myMetaFileGuidCache;

        private readonly CompactOneToListMap<string, IPsiSourceFile> myReferencedElementToAsset =
            new CompactOneToListMap<string, IPsiSourceFile>();

        public UnityEventHandlerReferenceCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager,
                                               MetaFileGuidCache metaFileGuidCache)
            : base(lifetime, persistentIndexManager, CreateMarshaller())
        {
            myMetaFileGuidCache = metaFileGuidCache;
        }

        private static IUnsafeMarshaller<List<string>> CreateMarshaller()
        {
            return UnsafeMarshallers.GetCollectionMarshaller(UnsafeMarshallers.UnicodeStringMarshaller,
                n => new List<string>(n));
        }

        public bool IsEventHandler([NotNull] IDeclaredElement declaredElement)
        {
            var sourceFiles = declaredElement.GetSourceFiles();

            // The methods and property setters that we are interested in will only have a single source file
            if (sourceFiles.Count != 1)
                return false;

            var assetGuid = myMetaFileGuidCache.GetAssetGuid(sourceFiles[0]);
            if (assetGuid == null)
                return false;

            var referencedElementKey = GetReferencedElementKey(assetGuid, declaredElement);
            return myReferencedElementToAsset[referencedElementKey].Count > 0;
        }

        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<YamlProjectFileType>() &&
                   sourceFile.PsiModule is UnityExternalFilesPsiModule &&
                   UnityYamlFileExtensions.IsAsset(sourceFile.GetLocation());
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<YamlLanguage>() as IYamlFile;
            if (file == null)
                return null;

            var referencedElements = new List<string>();
            var referenceProcessor = new ConditionalRecursiveReferenceProcessor(reference =>
            {
                var assetGuid = reference.GetScriptAssetGuid();
                if (assetGuid != null)
                {
                    var referencedElementKey = GetReferencedElementKey(assetGuid, reference.EventHandlerName);
                    if (referencedElementKey != null)
                        referencedElements.Add(referencedElementKey);
                }
            });

            foreach (var document in file.DocumentsEnumerable)
            {
                if (UnityEventTargetReferenceFactory.CanContainReference(document))
                    referenceProcessor.ProcessForResolve(document);
            }

            return referencedElements.Count > 0 ? referencedElements : null;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
            AddToLocalCache(sourceFile, builtPart as List<string> ?? EmptyList<string>.InstanceList);
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
            foreach (var (sourceFile, cacheItem) in Map)
                AddToLocalCache(sourceFile, cacheItem);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, [NotNull] IEnumerable<string> referencedElementKeys)
        {
            foreach (var referencedElementKey in referencedElementKeys)
                myReferencedElementToAsset.AddValue(referencedElementKey, sourceFile);
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var referencedElementKeys))
            {
                foreach (var referencedElementKey in referencedElementKeys)
                    myReferencedElementToAsset.RemoveValue(referencedElementKey, sourceFile);
            }
        }

        [CanBeNull]
        private string GetReferencedElementKey(string assetGuid, IDeclaredElement declaredElement)
        {
            switch (declaredElement)
            {
                case IMethod method:
                    return GetReferencedElementKey(assetGuid, method.ShortName);

                case IProperty property:
                    return GetReferencedElementKey(assetGuid, property.Setter?.ShortName);
            }

            return null;
        }

        private string GetReferencedElementKey(string assetGuid, [CanBeNull] string handlerName)
        {
            if (handlerName == null)
                return null;

            return assetGuid + "::" + handlerName;
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