using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [SolutionComponent]
    public class ProjectSettingsCache : SimpleICache<ProjectSettingsCacheItem>
    {
        private readonly IEnumerable<IProjectSettingsAssetHandler> myProjectSettingsAssetHandlers;
        private readonly ProjectSettingsCacheItem myLocalCache = new ProjectSettingsCacheItem();


        public ProjectSettingsCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager,
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
            }
        }
    }
}