using System.Collections.Generic;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Text;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.UnityEditorPropertyValues
{
    [PsiComponent]
    public class UnityPropertyValueCache : SimpleICache<UnitySceneData>
    {
        public readonly UnitySceneDataLocalCache UnitySceneDataLocalCache;
        
        public UnityPropertyValueCache(Lifetime lifetime, MetaFileGuidCache metaFileGuidCache, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, UnitySceneData.Marshaller)
        {
            UnitySceneDataLocalCache = new UnitySceneDataLocalCache(metaFileGuidCache);
        }
        
        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.PsiModule is UnityExternalFilesPsiModule && 
                   base.IsApplicable(sourceFile) &&
                   sourceFile.LanguageType.Is<UnityYamlProjectFileType>() &&
                   sourceFile.IsAsset();
        }


        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            // If YAML parsing is disabled, this will return null
            var file = sourceFile.GetDominantPsiFile<UnityYamlLanguage>() as IUnityYamlFile;
            if (file == null)
                return null;

            return UnitySceneData.Build(file);
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            base.Merge(sourceFile, builtPart);
            if (builtPart is UnitySceneData sceneData)
            {
                AddToLocalCache(sourceFile, sceneData);
            }
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
            foreach (var (file, cacheItems) in Map)
                AddToLocalCache(file, cacheItems);
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, UnitySceneData sceneData)
        {
            UnitySceneDataLocalCache.Add(sourceFile, sceneData);
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var sceneData))
            {
                UnitySceneDataLocalCache.Remove(sourceFile, sceneData);
            }
        }
    }
}