using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [SolutionComponent]
    public class UnityExternalFilesModuleProcessor : IChangeProvider, IUnityReferenceChangeHandler
    {
        private const ulong AssetFileCheckSizeThreshold = 20 * (1024 * 1024); // 20 MB

        // stats
        public readonly List<ulong> PrefabSizes = new();
        public readonly List<ulong> SceneSizes = new();
        public readonly List<ulong> AssetSizes = new();
        public readonly List<ulong> KnownBinaryAssetSizes = new();
        public readonly List<ulong> ExcludedByNameAssetsSizes = new();

        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ChangeManager myChangeManager;
        private readonly IShellLocks myLocks;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly UnityExternalPsiSourceFileFactory myPsiSourceFileFactory;
        private readonly UnityExternalFilesModuleFactory myModuleFactory;
        private readonly UnityExternalFilesIndexDisablingStrategy myIndexDisablingStrategy;
        private readonly JetHashSet<VirtualFileSystemPath> myRootPaths;
        private readonly VirtualFileSystemPath mySolutionDirectory;

        public UnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution,
                                                 ChangeManager changeManager,
                                                 IShellLocks locks,
                                                 ISolutionLoadTasksScheduler scheduler,
                                                 IFileSystemTracker fileSystemTracker,
                                                 UnityExternalPsiSourceFileFactory psiSourceFileFactory,
                                                 UnityExternalFilesModuleFactory moduleFactory,
                                                 UnityExternalFilesIndexDisablingStrategy indexDisablingStrategy)
        {
            myLifetime = lifetime;
            myLogger = logger;
            myChangeManager = changeManager;
            myLocks = locks;
            myFileSystemTracker = fileSystemTracker;
            myPsiSourceFileFactory = psiSourceFileFactory;
            myModuleFactory = moduleFactory;
            myIndexDisablingStrategy = indexDisablingStrategy;

            changeManager.RegisterChangeProvider(lifetime, this);

            myRootPaths = new JetHashSet<VirtualFileSystemPath>();

            // SolutionDirectory isn't absolute in tests, and will throw an exception if we use it when we call Exists
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());

            scheduler.EnqueueTask(new SolutionLoadTask(GetType().Name + ".Activate",
                SolutionLoadTaskKinds.PreparePsiModules,
                () => myChangeManager.AddDependency(myLifetime, solution.PsiModules(), this)));
        }

        public void OnHasUnityReference()
        {
            // Do nothing
        }

        public virtual void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            // For project model access
            myLocks.AssertReadAccessAllowed();

            // Note that this means we process meta and asset files for class library projects, which likely won't have
            // asset files. We can't use IsUnityGeneratedProject because any project based in the Packages folder or
            // using 'file:' won't be processed.
            if (!project.IsUnityProject())
                return;

            var externalFiles = CollectExternalFilesForUnityProject();
            CollectExternalFilesForAsmDefProject(externalFiles, project);

            // Disable asset indexing for massive projects. Note that we still collect all files, and always index
            // project settings and meta files.
            if (externalFiles.AssetFiles.Count > 0) myIndexDisablingStrategy.Run(externalFiles.AssetFiles);

            AddExternalFiles(externalFiles);
            UpdateStatistics(externalFiles);
        }

        private ExternalFiles CollectExternalFilesForUnityProject()
        {
            // These are idempotent and can be called multiple times. We expect most files to come from here - either in
            // Assets or Packages. We can have assets in externally stored packages, but they are not treated as user
            // editable source, so we can mostly ignore them (the exception is file: based packages, but these are
            // expected to be a small number, and will complicate gathering stats for thresholds)
            var externalFiles = new ExternalFiles();
            CollectExternalFilesForSolutionDirectory(externalFiles, "Assets");
            CollectExternalFilesForSolutionDirectory(externalFiles, "Packages");
            CollectExternalFilesForSolutionDirectory(externalFiles, "ProjectSettings");
            return externalFiles;
        }

        private void CollectExternalFilesForSolutionDirectory(ExternalFiles externalFiles, string relativePath)
        {
            var path = mySolutionDirectory.Combine(relativePath);
            if (path.ExistsDirectory)
                CollectExternalFilesForDirectory(externalFiles, path);
        }

        // See if the project is based on a .asmdef file, and process the files at the .asmdef file location. We'll
        // already have processed any of these files in the Assets and Packages folder, so this will catch any
        // packages that are external to the solution folder and registered with `file:`
        private void CollectExternalFilesForAsmDefProject(ExternalFiles externalFiles, IProject project)
        {
            if (!project.IsProjectFromUserView())
                return;

            // We know that the default projects are not .asmdef based, and will obviously live in Assets, which we've
            // already processed. This is just an optimisation - if a plugin renames the projects, we'll still work ok
            if (project.IsOneOfPredefinedUnityProjects())
            {
                return;
            }

            foreach (var projectItem in project.GetSubItemsRecursively())
            {
                if (projectItem is IProjectFile projectFile)
                {
                    var location = projectFile.Location;
                    if (location.FullPath.EndsWith(".asmdef", StringComparison.InvariantCultureIgnoreCase))
                    {
                        CollectExternalFilesForDirectory(externalFiles, location.Parent);
                        return;
                    }
                }
            }
        }

        private void CollectExternalFilesForDirectory(ExternalFiles externalFiles, VirtualFileSystemPath directory)
        {
            // Don't process the entire solution directory - this would process Assets and Packages for a second time,
            // and also process Temp and Library, which are likely to be huge
            if (directory == mySolutionDirectory)
            {
                myLogger.Error("Unexpected request to process entire solution directory. Skipping");
                return;
            }

            if (myRootPaths.Contains(directory))
                return;

            // Make sure the directory hasn't already been processed as part of a previous directory. This can happen if
            // the project is a .asmdef based project living under Assets or Packages, or inside a file:// based package
            foreach (var rootPath in myRootPaths)
            {
                if (rootPath.IsPrefixOf(directory))
                    return;
            }

            myLogger.Info("Processing directory for asset and meta files: {0}", directory);

            // Based on super simple tests, GetDirectoryEntries is faster than GetChildFiles with subsequent calls to
            // GetFileLength. But what is more surprising is that Windows in a VM is an order of magnitude FASTER than
            // Mac, on the same project!
            var entries = directory.GetDirectoryEntries("*", PathSearchFlags.RecurseIntoSubdirectories
                                                             | PathSearchFlags.ExcludeDirectories);
            foreach (var entry in entries)
                externalFiles.AddFile(entry);
            externalFiles.AddDirectory(directory);

            myRootPaths.Add(directory);
        }

        // We add scenes, assets and prefabs to the Misc Files project in Rider. This is so that:
        // * The Find Usages results list expects project files and throws if any occurrence is a project file
        // * Only project files are included in SWEA, and we want that for the usage count Code Vision metric
        // Unfortunately, ReSharper keeps the Misc Files project in sync with Visual Studio's idea of the Misc Files
        // project (i.e. files open in the editor that aren't part of a project). This means ReSharper will remove our
        // files from Misc Files and we end up with invalid PSI source files and loads of exceptions.
        // Fortunately, ReSharper doesn't require project files for find usages or rename, and doesn't have Code Vision,
        // so we don't need to worry about a usage count (usage suppression is already handled in the suppressor). So
        // for ReSharper, we just treat all of our files as PSI source files
        private void AddExternalFiles(ExternalFiles externalFiles)
        {
            var builder = new PsiModuleChangeBuilder();
            AddExternalPsiSourceFiles(externalFiles.MetaFiles, builder);
            AddExternalPsiSourceFiles(externalFiles.AssetFiles, builder);
            FlushChanges(builder);

            // We should only start watching for file system changes after adding the files we know about
            foreach (var directory in externalFiles.Directories)
                myFileSystemTracker.AdviseDirectoryChanges(myLifetime, directory, true, OnProjectDirectoryChange);
        }

        private void AddExternalPsiSourceFiles(List<VirtualDirectoryEntryData> files, PsiModuleChangeBuilder builder)
        {
            foreach (var directoryEntry in files)
                 AddOrUpdateExternalPsiSourceFile(builder, directoryEntry.GetAbsolutePath());
        }

        private void AddOrUpdateExternalPsiSourceFile(PsiModuleChangeBuilder builder, VirtualFileSystemPath path)
        {
            Assertion.AssertNotNull(myModuleFactory.PsiModule, "myModuleFactory.PsiModule != null");

            var sourceFile = GetYamlPsiSourceFile(myModuleFactory.PsiModule, path);
            if (sourceFile != null)
            {
                // We already know this file. Make sure it's up to date
                UpdateExternalPsiSourceFile(sourceFile, builder, path);
                return;
            }

            sourceFile = myPsiSourceFileFactory.CreateExternalPsiSourceFile(myModuleFactory.PsiModule, path);
            builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Added);
        }

        private static void UpdateExternalPsiSourceFile([CanBeNull] IPsiSourceFile sourceFile,
                                                        PsiModuleChangeBuilder builder,
                                                        VirtualFileSystemPath path)
        {
            if (sourceFile == null) return;

            // Make sure we update the cached file system data, or all of the ICache implementations will think the
            // file is already up to date
            (sourceFile as PsiSourceFileFromPath)?.GetCachedFileSystemData().Refresh(path);
            builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Modified);
        }

        private void UpdateStatistics(ExternalFiles externalFiles)
        {
            foreach (var directoryEntry in externalFiles.AssetFiles)
            {
                if (directoryEntry.RelativePath.IsAsset())
                    AssetSizes.Add(directoryEntry.Length);
                else if (directoryEntry.RelativePath.IsPrefab())
                    PrefabSizes.Add(directoryEntry.Length);
                else if (directoryEntry.RelativePath.IsScene())
                    SceneSizes.Add(directoryEntry.Length);
            }

            foreach (var directoryEntry in externalFiles.KnownBinaryAssetFiles)
                KnownBinaryAssetSizes.Add(directoryEntry.Length);

            foreach (var directoryEntry in externalFiles.ExcludedByNameAssetFiles)
                ExcludedByNameAssetsSizes.Add(directoryEntry.Length);
        }

        private static bool IsEligibleAsset(VirtualFileSystemPath path)
        {
            return path.IsInterestingAsset() && !IsKnownBinaryAsset(path) && !IsAssetExcludedByName(path);
        }

        private static bool IsKnownBinaryAsset(VirtualDirectoryEntryData directoryEntry)
        {
            if (IsKnownBinaryAssetByName(directoryEntry.RelativePath))
                return true;

            if (directoryEntry.Length > AssetFileCheckSizeThreshold && directoryEntry.RelativePath.IsAsset())
                return !directoryEntry.GetAbsolutePath().SniffYamlHeader();
            return false;
        }

        private static bool IsKnownBinaryAsset(VirtualFileSystemPath path)
        {
            if (IsKnownBinaryAssetByName(path))
                return true;

            var fileLength = (ulong) path.GetFileLength();
            if (fileLength > AssetFileCheckSizeThreshold && path.IsAsset())
                return !path.SniffYamlHeader();
            return false;
        }

        private static bool IsKnownBinaryAssetByName(IPath path)
        {
            // Even if the project is set to ForceText, some files will always be binary, notably LightingData.asset.
            // Users can also force assets to serialise as binary with the [PreferBinarySerialization] attribute
            var filename = path.Name;
            if (filename.Equals("LightingData.asset", StringComparison.InvariantCultureIgnoreCase))
                return true;
            return false;
        }

        private static bool IsAssetExcludedByName(IPath path)
        {
            // NavMesh.asset can sometimes be binary, sometimes text. I don't know the criteria for when one format is
            // picked over another. OcclusionCullingData.asset is usually text, but large and contains long streams of
            // ascii-based "binary". Neither file contains anything we're interested in, and simply increases parsing
            // and indexing time
            var filename = path.Name;
            return filename.Equals("NavMesh.asset", StringComparison.InvariantCultureIgnoreCase)
                || filename.Equals("OcclusionCullingData.asset", StringComparison.InvariantCultureIgnoreCase);
        }

        private void OnProjectDirectoryChange(FileSystemChangeDelta delta)
        {
            myLocks.ExecuteOrQueue(Lifetime.Eternal, "UnityExternalFilesModuleProcessor::OnProjectDirectoryChange",
                () =>
                {
                    var builder = new PsiModuleChangeBuilder();
                    var projectFilesToAdd = new List<VirtualFileSystemPath>();
                    ProcessFileSystemChangeDelta(delta, builder, projectFilesToAdd);
                    FlushChanges(builder);
                });
        }

        private void ProcessFileSystemChangeDelta(FileSystemChangeDelta delta, PsiModuleChangeBuilder builder,
            List<VirtualFileSystemPath> projectFilesToAdd)
        {
            var module = myModuleFactory.PsiModule;
            if (module == null)
                return;

            IPsiSourceFile sourceFile;
            switch (delta.ChangeType)
            {
                // We can get ADDED for a file we already know about if an app saves the file by saving to a temp file
                // first. We don't get a DELETED first, surprisingly. Treat this scenario like CHANGED
                case FileSystemChangeType.ADDED:
                    if (IsEligibleAsset(delta.NewPath))
                    {
                        AddOrUpdateExternalPsiSourceFile(builder, delta.NewPath);
                        projectFilesToAdd.Add(delta.NewPath);
                    }
                    else if (delta.NewPath.IsMeta())
                        AddOrUpdateExternalPsiSourceFile(builder, delta.NewPath);
                    break;

                case FileSystemChangeType.DELETED:
                    sourceFile = GetYamlPsiSourceFile(module, delta.OldPath);
                    if (sourceFile != null)
                        builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Removed);
                    break;

                // We can get RENAMED if an app saves the file by saving to a temporary name first, then renaming
                case FileSystemChangeType.CHANGED:
                case FileSystemChangeType.RENAMED:
                    sourceFile = GetYamlPsiSourceFile(module, delta.NewPath);
                    UpdateExternalPsiSourceFile(sourceFile, builder, delta.NewPath);
                    break;

                case FileSystemChangeType.SUBTREE_CHANGED:
                case FileSystemChangeType.UNKNOWN:
                    break;
            }

            foreach (var child in delta.GetChildren())
                ProcessFileSystemChangeDelta(child, builder, projectFilesToAdd);
        }

        [CanBeNull]
        private static IPsiSourceFile GetYamlPsiSourceFile([NotNull] IPsiModuleOnFileSystemPaths module,
                                                           VirtualFileSystemPath path)
        {
            return module.TryGetFileByPath(path, out var sourceFile) ? sourceFile : null;
        }

        private void FlushChanges(PsiModuleChangeBuilder builder)
        {
            if (builder.IsEmpty)
                return;

            myLocks.ExecuteOrQueueEx(myLifetime, GetType().Name + ".FlushChanges",
                () =>
                {
                    var module = myModuleFactory.PsiModule;
                    Assertion.AssertNotNull(module, "module != null");
                    myLocks.AssertMainThread();
                    using (myLocks.UsingWriteLock())
                    {
                        var psiModuleChange = builder.Result;
                        foreach (var fileChange in psiModuleChange.FileChanges)
                        {
                            var location = fileChange.Item.GetLocation();
                            if (location.IsEmpty)
                                continue;

                            switch (fileChange.Type)
                            {
                                case PsiModuleChange.ChangeType.Added:
                                    module.Add(location, fileChange.Item, null);
                                    break;

                                case PsiModuleChange.ChangeType.Removed:
                                    module.Remove(location);
                                    break;
                            }
                        }

                        myChangeManager.OnProviderChanged(this, psiModuleChange, SimpleTaskExecutor.Instance);
                    }
                });
        }

        public object Execute(IChangeMap changeMap) => null;

        private class ExternalFiles
        {
            public readonly List<VirtualDirectoryEntryData> MetaFiles = new();
            public readonly List<VirtualDirectoryEntryData> AssetFiles = new();
            public FrugalLocalList<VirtualDirectoryEntryData> KnownBinaryAssetFiles;
            public FrugalLocalList<VirtualDirectoryEntryData> ExcludedByNameAssetFiles;
            public FrugalLocalList<VirtualFileSystemPath> Directories;

            public void AddFile(VirtualDirectoryEntryData directoryEntry)
            {
                if (directoryEntry.RelativePath.IsMeta())
                    MetaFiles.Add(directoryEntry);
                else if (directoryEntry.RelativePath.IsInterestingAsset())
                {
                    if (IsKnownBinaryAsset(directoryEntry))
                        KnownBinaryAssetFiles.Add(directoryEntry);
                    else if (IsAssetExcludedByName(directoryEntry.RelativePath))
                        ExcludedByNameAssetFiles.Add(directoryEntry);
                    else
                        AssetFiles.Add(directoryEntry);
                }
            }

            public void AddDirectory(VirtualFileSystemPath directory)
            {
                Directories.Add(directory);
            }
        }
    }
}