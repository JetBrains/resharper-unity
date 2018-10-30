using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.DataFlow;
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
        private readonly CompactOneToListMap<string, FileSystemPath> myAssetGuidToAssetFilePaths = new CompactOneToListMap<string, FileSystemPath>();

        public MetaFileGuidCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, MetaFileCacheItem.Marshaller)
        {
#if DEBUG
            ClearOnLoad = true;
#endif
        }

        // This will usually return a single value, but there's always a chance for copy/paste
        // Also note that this returns the file path of the asset associated with the GUID, not the asset's .meta file!
        public IList<FileSystemPath> GetAssetFilePathsFromGuid(string guid)
        {
            return myAssetGuidToAssetFilePaths[guid];
        }

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.IsLanguageSupported<YamlLanguage>() &&
                   sf.Name.EndsWith(".cs.meta", StringComparison.InvariantCultureIgnoreCase);
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            if (!(sourceFile.GetDominantPsiFile<YamlLanguage>() is IYamlFile yamlFile))
                return null;

            var document = yamlFile.Documents.FirstOrDefault();
            if (document?.BlockNode is IBlockMappingNode blockMappingNode)
            {
                foreach (var entry in blockMappingNode.Entries)
                {
                    if (entry.Key is IPlainScalarNode keyScalarNode && keyScalarNode.Text?.GetText() == "guid")
                    {
                        if (entry.Value is IPlainScalarNode valueScalarNode)
                        {
                            var guid = valueScalarNode.Text?.GetText();
                            if (guid != null)
                                return new MetaFileCacheItem(guid);
                        }
                    }
                }
            }

            return null;
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            if (builtPart == null)
                CleanLocalCache(sourceFile);

            base.Merge(sourceFile, builtPart);
            PopulateLocalCache(sourceFile, builtPart as MetaFileCacheItem);
        }

        public override void MergeLoaded(object data)
        {
            base.MergeLoaded(data);

            foreach (var (psiSourceFile, cacheItem) in Map)
                PopulateLocalCache(psiSourceFile, cacheItem);
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            CleanLocalCache(sourceFile);
            base.Drop(sourceFile);
        }

        private void PopulateLocalCache(IPsiSourceFile sourceFile, [CanBeNull] MetaFileCacheItem data)
        {
            if (data == null) return;

            var metaFileLocation = sourceFile.GetLocation();
            if (!metaFileLocation.IsEmpty)
            {
                var assetLocation = GetAssetLocationFromMetaFile(metaFileLocation);
                myAssetGuidToAssetFilePaths.AddValue(data.Guid, assetLocation);
            }
        }

        private void CleanLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItem))
            {
                var assetLocation = GetAssetLocationFromMetaFile(sourceFile.GetLocation());
                myAssetGuidToAssetFilePaths.RemoveValue(cacheItem.Guid, assetLocation);
            }
        }

        private static FileSystemPath GetAssetLocationFromMetaFile(FileSystemPath metaFileLocation)
        {
            return metaFileLocation.ChangeExtension(string.Empty);
        }
    }
}