using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches
{
    [PsiComponent]
    public class ResourceLoadCache : SimpleICache<ResourcesCacheItem>
    {
        private const string ResourcesFolderName = "Resources";
        private const string EditorFolderName = "Editor";

        private readonly ISolution mySolution;
        private readonly string myDriveRootDirForSolution;


        public ResourceLoadCache(Lifetime lifetime,
            [NotNull] IShellLocks locks,
            [NotNull] IPersistentIndexManager persistentIndexManager,
            ISolution solution)
            : base(lifetime, locks, persistentIndexManager, ResourcesCacheItem.Marshaller)
        {
            mySolution = solution;
            myDriveRootDirForSolution = mySolution.SolutionDirectory.GetRootDir();
        }

        public HashSet<ResourceCacheInfo> CachedResources { get; } = new();


        protected override bool IsApplicable(IPsiSourceFile sourceFile)
        {
            return sourceFile.Name.EndsWith(".meta", StringComparison.InvariantCultureIgnoreCase);
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            if (!IsApplicable(sourceFile))
                return null;

            var sourceFileLocation = sourceFile.GetLocation();

            sourceFileLocation = sourceFileLocation.ChangeExtension(""); //eliminate .meta
            var extensionNoDot = sourceFileLocation.ExtensionNoDot;

            if (extensionNoDot.IsNullOrEmpty()) //files without extension or any folder - should be skipped 
                return null;

            var driveRootDirForSourceFile = sourceFileLocation.GetRootDir();

            var isOnTheSameDiskAsSolution = driveRootDirForSourceFile == myDriveRootDirForSolution;

            var relativeSourceFilePath = isOnTheSameDiskAsSolution
                ? sourceFileLocation.MakeRelativeTo(mySolution.SolutionDirectory).ChangeExtension("")
                : sourceFileLocation.MakeRelativeTo(VirtualFileSystemPath.Parse(driveRootDirForSourceFile, InteractionContext.SolutionContext)).ChangeExtension("");

            Assertion.Assert(relativeSourceFilePath != null, nameof(relativeSourceFilePath) + " != null");

            var unityResourceFilePath = GetPathInsideResourcesFolder(relativeSourceFilePath);
            if (unityResourceFilePath.IsEmpty) return null;

            var inAssetsFolder = relativeSourceFilePath.StartsWith("Assets");

            var distanceToResourcesFolder = GetDistanceToParentFolder(relativeSourceFilePath, ResourcesFolderName);
            var distanceToEditorFolder = GetDistanceToParentFolder(relativeSourceFilePath, EditorFolderName);

            //Resources/Editor/asset.png -> Editor/asset.png RUNTIME
            //Editor/Resources/asset.png -> asset.png EDITOR
            var isEditorResource = distanceToEditorFolder >= 0 && distanceToEditorFolder > distanceToResourcesFolder;

            if (inAssetsFolder)
            {
                return new ResourcesCacheItem(
                    isEditorResource ? ResourceLocationType.Editor : ResourceLocationType.Player,
                    sourceFileLocation,
                    unityResourceFilePath,
                    extensionNoDot);
            }

            return new ResourcesCacheItem(
                isEditorResource ? ResourceLocationType.PackageEditor : ResourceLocationType.PackagePlayer,
                sourceFileLocation,
                unityResourceFilePath,
                extensionNoDot);
        }

        private static RelativePath GetPathInsideResourcesFolder(RelativePath relativeSourceFilePath)
        {
            //Assets/Resources/Folder/img.png
            //Assets/Resources/Folder/Resources/img.png -> img.png 
            var parent = relativeSourceFilePath.Parent;

            int sanityCheck = 10000;
            while (!parent.IsEmpty && --sanityCheck > 0)
            {
                if (parent.Name == ResourcesFolderName)
                    return relativeSourceFilePath.MakeRelativeTo(parent);
                parent = parent.Parent;
            }

            Assertion.Assert(sanityCheck > 0, "Possible infinite loop");            

            return RelativePath.Empty;
        }

        private static int GetDistanceToParentFolder(RelativePath relativeSourceFilePath, string folderName)
        {
            var distance = 1;
            var parent = relativeSourceFilePath.Parent;

            int sanityCheck = 10000;
            while (!parent.IsEmpty && --sanityCheck > 0)
            {
                if (parent.Name == folderName)
                    return distance;
                parent = parent.Parent;
                ++distance;
            }

            Assertion.Assert(sanityCheck > 0, "Possible infinite loop");

            return -1;
        }


        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            RemoveFromLocalCache(sourceFile);
            AddToLocalCache(builtPart as ResourcesCacheItem);
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
            foreach (var (_, cacheItem) in Map)
                AddToLocalCache(cacheItem);
        }

        private void AddToLocalCache([CanBeNull] ResourcesCacheItem cacheItem)
        {
            if (cacheItem == null) return;
            CachedResources.Add(
                new ResourceCacheInfo(cacheItem.LocationType,
                    cacheItem.RelativePath,
                    cacheItem.PathInsideResourcesFolder,
                    cacheItem.ExtensionWithDot));
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItem))
            {
                CachedResources.Remove(
                    new ResourceCacheInfo(cacheItem.LocationType,
                        cacheItem.RelativePath,
                        cacheItem.PathInsideResourcesFolder,
                        cacheItem.ExtensionWithDot));
            }
        }
        
        public readonly struct ResourceCacheInfo
        {
            public readonly ResourceLocationType ResourceLocationType;
            public readonly RelativePath RelativePath;
            [CanBeNull] public readonly VirtualFileSystemPath VirtualFileSystemPath;
            public readonly string Extension;

            public ResourceCacheInfo(ResourceLocationType resourceLocationType,
                RelativePath relativePath,
                VirtualFileSystemPath virtualFileSystemPath,
                string extension)
            {
                ResourceLocationType = resourceLocationType;
                RelativePath = relativePath;
                VirtualFileSystemPath = virtualFileSystemPath;
                Extension = extension;
            }

            public bool Equals(ResourceCacheInfo other)
            {
                return ResourceLocationType == other.ResourceLocationType && Equals(RelativePath, other.RelativePath) &&
                       Equals(VirtualFileSystemPath, other.VirtualFileSystemPath) && Extension == other.Extension;
            }

            public override bool Equals(object obj)
            {
                return obj is ResourceCacheInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int)ResourceLocationType;
                    hashCode = (hashCode * 397) ^ (RelativePath != null ? RelativePath.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^
                               (VirtualFileSystemPath != null ? VirtualFileSystemPath.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Extension != null ? Extension.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}