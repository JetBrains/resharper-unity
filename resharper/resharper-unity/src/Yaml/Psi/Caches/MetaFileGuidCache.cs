using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [PsiComponent]
    public class MetaFileGuidCache : SimpleICache<MetaFileCacheItem>
    {
        private readonly ILogger myLogger;

        // We expect to only get one asset with a given guid, but copy/pasting .meta files could break that.
        // CompactOneToListMap is optimised for the typical use case of only one item per key
        private readonly CompactOneToListMap<Guid, FileSystemPath> myAssetGuidToAssetFilePaths =
            new CompactOneToListMap<Guid, FileSystemPath>();

        // Note that Map is a map of *meta file* to asset guid, NOT asset file!
        private readonly Dictionary<FileSystemPath, Guid> myAssetFilePathToGuid =
            new Dictionary<FileSystemPath, Guid>();

        public Signal<(IPsiSourceFile sourceFile, Guid? oldGuid, Guid? newGuid)> GuidChanged =
            new Signal<(IPsiSourceFile sourceFile, Guid? oldGuid, Guid? newGuid)>("GuidChanged");

        public MetaFileGuidCache(Lifetime lifetime, IShellLocks shellLocks, IPersistentIndexManager persistentIndexManager, ILogger logger)
            : base(lifetime, shellLocks, persistentIndexManager, MetaFileCacheItem.Marshaller)
        {
            myLogger = logger;
        }

        // This will usually return a single value, but there's always a chance for copy/paste
        // Also note that this returns the file path of the asset associated with the GUID, not the asset's .meta file!
        public IList<FileSystemPath> GetAssetFilePathsFromGuid(Guid guid)
        {
            return myAssetGuidToAssetFilePaths[guid];
        }

        public IList<string> GetAssetNames(Guid guid)
        {
            return myAssetGuidToAssetFilePaths[guid].Select(p => p.NameWithoutExtension).ToList();
        }

        [CanBeNull]
        public Guid? GetAssetGuid(IPsiSourceFile sourceFile)
        {
            return myAssetFilePathToGuid.TryGetValue(sourceFile.GetLocation(), out var guid) ? guid : (Guid?) null;
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.Name.EndsWith(".meta", StringComparison.InvariantCultureIgnoreCase) && sf.PsiModule is UnityExternalFilesPsiModule;
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            if (!(sourceFile.GetDominantPsiFile<YamlLanguage>() is IYamlFile yamlFile))
                return null;

            // Note that this opens the document body chameleon, but we don't care for .meta files. They're lightweight
            var document = yamlFile.Documents.FirstOrDefault();
            if (document?.Body.BlockNode is IBlockMappingNode blockMappingNode)
            {
                var guid = blockMappingNode.GetMapEntryPlainScalarText("guid");
                if (guid != null && Guid.TryParse(guid, out var rGuid))
                    return new MetaFileCacheItem(rGuid);
            }

            return null;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            try
            {
                var oldValue = Map.GetValueSafe(sourceFile)?.Guid;
                var newValue = (builtPart as MetaFileCacheItem)?.Guid;
                if (!string.Equals(oldValue, newValue))
                    GuidChanged.Fire((sourceFile, oldValue, newValue));
            }
            catch (Exception e)
            {
                myLogger.Error(e, "An error occured during changing guid");
            }

            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(sourceFile, builtPart as MetaFileCacheItem);
            base.Merge(sourceFile, builtPart);
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);
            PopulateLocalCache();
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            try
            {
                var oldValue = Map.GetValueSafe(sourceFile)?.Guid;
                GuidChanged.Fire((sourceFile, oldValue, null));
            }
            catch (Exception e)
            {
                myLogger.Error(e, "An error occured during changing guid");
            }

            RemoveFromLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache()
        {
            foreach (var (psiSourceFile, cacheItem) in Map)
                AddToLocalCache(psiSourceFile, cacheItem);
        }

        private void AddToLocalCache(IPsiSourceFile metaFile, [CanBeNull] MetaFileCacheItem cacheItem)
        {
            if (cacheItem == null) return;

            var metaFileLocation = metaFile.GetLocation();
            if (!metaFileLocation.IsEmpty)
            {
                var assetLocation = GetAssetLocationFromMetaFile(metaFileLocation);
                myAssetGuidToAssetFilePaths.AddValue(cacheItem.Guid, assetLocation);

                if (myAssetFilePathToGuid.ContainsKey(assetLocation))
                {
                    // That error means, that merge/drop event ordering is incorrect or file is added twice to UnityExternalFilesPsiModule
                    // Order of merge/drop events are matter for move refactoring, drop should be first, because we should remove assetLocation
                    // from myAssetFilePathToGuid for old psiSourceFile (file in old folder)
                    // and add it again for new psiSourceFile (file in new folder)
                    myLogger.Error($"{assetLocation.Name} has been already added to myAssetFilePathToGuid, replacing old guid : {myAssetFilePathToGuid[assetLocation]} by {cacheItem.Guid}");
                }
                myAssetFilePathToGuid[assetLocation] = cacheItem.Guid;
            }
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItem))
            {
                var assetLocation = GetAssetLocationFromMetaFile(sourceFile.GetLocation());
                myAssetGuidToAssetFilePaths.RemoveValue(cacheItem.Guid, assetLocation);
                myAssetFilePathToGuid.Remove(assetLocation);
            }
        }

        private static FileSystemPath GetAssetLocationFromMetaFile(FileSystemPath metaFileLocation)
        {
            return metaFileLocation.ChangeExtension(string.Empty);
        }
    }
}