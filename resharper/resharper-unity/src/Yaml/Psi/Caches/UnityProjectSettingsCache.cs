using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util.Collections;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class UnityProjectSettingsCache : SimpleICache<ProjectSettingsCacheItem>
    {
        private readonly IEnumerable<IProjectSettingsAssetHandler> myProjectSettingsAssetHandlers;
        private readonly ProjectSettingsCacheItem myLocalCache = new ProjectSettingsCacheItem();
        private readonly CountingSet<string> myShortNameSceneCount = new CountingSet<string>();

        public UnityProjectSettingsCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager,
            IEnumerable<IProjectSettingsAssetHandler> projectSettingsAssetHandlers)
            : base(lifetime, persistentIndexManager, ProjectSettingsCacheItem.Marshaller)
        {
            myProjectSettingsAssetHandlers = projectSettingsAssetHandlers;
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

            foreach (var projectSettingsAssetHandler in myProjectSettingsAssetHandlers)
            {
                if (projectSettingsAssetHandler.IsApplicable(sourceFile))
                    return projectSettingsAssetHandler.Build(sourceFile);
            }

            return null;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as ProjectSettingsCacheItem);
            base.Merge(sourceFile, builtPart);
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
            foreach (var (file, cacheItem) in Map)
            {
                AddToLocalCache(file, cacheItem);
            }
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItem))
            {
                foreach (var (name, count) in cacheItem.SceneNames)
                {
                    myLocalCache.SceneNames.Add(name, -count);
                }
            }
        }

        private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] ProjectSettingsCacheItem cacheItem)
        {
            if (cacheItem == null)
                return;

            foreach (var (name, count) in cacheItem.SceneNames)
            {
                myLocalCache.SceneNames.Add(name, count);
                myShortNameSceneCount.Add(name.Split('/').Last().RemoveEnd(".unity"));
            }
        }

        public IEnumerable<string> GetAllScenesFromBuildSettings()
        {
            return myLocalCache.SceneNames.GetItems();
        }

        public int SceneCount => myLocalCache.SceneNames.Count;

        public bool IsScenePresentedAtEditorBuildSettings(string sceneName, out bool ambiguousDefinition)
        {
            ambiguousDefinition = false;
            var shortCount = myShortNameSceneCount.GetCount(sceneName);
            if (shortCount > 1)
            {
                ambiguousDefinition = true;
                return true;
            }

            if (shortCount == 1)
                return true;

            return myLocalCache.SceneNames.GetCount(sceneName) > 0;
        }
    }
}