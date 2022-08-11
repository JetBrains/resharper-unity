using System;
using System.Collections.Generic;
using System.Linq;
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

        public ResourceLoadCache(Lifetime lifetime,
            [NotNull] IShellLocks locks,
            [NotNull] IPersistentIndexManager persistentIndexManager,
            ISolution solution)
            : base(lifetime, locks, persistentIndexManager, ResourcesCacheItem.Marshaller)
        {
            mySolution = solution;
            
            // SolutionDirectory isn't absolute in tests, and will throw if used with FileSystemTracker
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());
        }

        private readonly object myCachedResourcesLock = new();
        private readonly HashSet<ResourceCacheInfo> myCachedResources = new();
        private readonly VirtualFileSystemPath mySolutionDirectory;

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
            var extensionNoDot =
                sourceFileLocation.ExtensionNoDot; //get real file extension (png) - used on the completion popup

            if (extensionNoDot.IsNullOrEmpty()) //files without extension or any folder - should be skipped 
                return null;

            var relativeToSolution = sourceFileLocation.TryMakeRelativeTo(mySolutionDirectory);

            var unityResourceFilePath =
                GetPathInsideResourcesFolder(relativeToSolution); //this path will be used in the completion
            if (unityResourceFilePath.IsEmpty) return null;

            unityResourceFilePath = unityResourceFilePath.ChangeExtension("");

            // relativeToSolution.IsAbsolute - for packages located outside of the project and loaded from the disk
            // StartsWith("Assets") - only for the assets inside Unity projects - packages excluded
            // ! StartsWith("Assets") - for packages only
            var inAssetsFolder = relativeToSolution.IsAbsolute
                ? false
                : relativeToSolution.AsRelative().StartsWith("Assets");

            //determine if editor or runtime resource
            var distanceToResourcesFolder = GetDistanceToParentFolder(relativeToSolution, ResourcesFolderName);
            var distanceToEditorFolder = GetDistanceToParentFolder(relativeToSolution, EditorFolderName);

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

        private static RelativePath GetPathInsideResourcesFolder(IPath relativeSourceFilePath)
        {
            //Assets/Resources/Folder/img.png
            //Assets/Resources/Folder/Resources/img.png -> img.png

            var parent = relativeSourceFilePath.Parent;

            while (!parent.IsEmpty)
            {
                if (parent.Name == ResourcesFolderName)
                {
                    if (!relativeSourceFilePath.IsAbsolute)
                        return relativeSourceFilePath.AsRelative().MakeRelativeTo(parent.AsRelative());

                    //AsAbsolut for VirtualFileSystemPath - throws an exception
                    if (relativeSourceFilePath is VirtualFileSystemPath virtualPath)
                        return virtualPath.MakeRelativeTo(parent as VirtualFileSystemPath);

                    return relativeSourceFilePath.AsAbsolute().MakeRelativeTo(parent.AsAbsolute());
                }

                parent = parent.Parent;
            }

            return RelativePath.Empty;
        }

        private static int GetDistanceToParentFolder(IPath relativeSourceFilePath, string folderName)
        {
            var distance = 1;
            var parent = relativeSourceFilePath.Parent;

            while (!parent.IsEmpty)
            {
                if (parent.Name == folderName)
                    return distance;
                parent = parent.Parent;
                ++distance;
            }

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
            lock (myCachedResourcesLock)
            {
                myCachedResources.Add(
                    new ResourceCacheInfo(cacheItem.LocationType,
                        cacheItem.RelativePath,
                        cacheItem.PathInsideResourcesFolder,
                        cacheItem.ExtensionWithDot));
            }
        }

        private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
        {
            if (Map.TryGetValue(sourceFile, out var cacheItem))
            {
                lock (myCachedResourcesLock)
                {
                    myCachedResources.Remove(
                        new ResourceCacheInfo(cacheItem.LocationType,
                            cacheItem.RelativePath,
                            cacheItem.PathInsideResourcesFolder,
                            cacheItem.ExtensionWithDot));
                }
            }
        }

        public bool HasResource(string literal)
        {
            lock (myCachedResourcesLock)
            {
                return myCachedResources.Any(item =>
                    item.RelativePath.NormalizeSeparators(FileSystemPathEx.SeparatorStyle.Unix)
                    == literal);
            }
        }

        public readonly struct ResourceCacheInfo
        {
            public readonly ResourceLocationType ResourceLocationType;
            public readonly RelativePath RelativePath;
            public readonly VirtualFileSystemPath VirtualFileSystemPath;
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

        public bool CollectItems(Func<ResourceCacheInfo, bool> collectAutocompletion)
        {
            var any = false;

            lock (myCachedResourcesLock)
            {
                foreach (var assetsFolderResource in myCachedResources)
                {
                    any |= collectAutocompletion(assetsFolderResource);
                }
            }

            return any;
        }
    }
}