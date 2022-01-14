using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [SolutionComponent]
    public class UnityExternalFilesModuleProcessor : IChangeProvider, IUnityReferenceChangeHandler
    {
        private const ulong AssetFileCheckSizeThreshold = 20 * (1024 * 1024); // 20 MB

        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly ChangeManager myChangeManager;
        private readonly PackageManager myPackageManager;
        private readonly IShellLocks myLocks;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly UnityExternalPsiSourceFileFactory myPsiSourceFileFactory;
        private readonly UnityExternalFilesModuleFactory myModuleFactory;
        private readonly UnityExternalFilesIndexDisablingStrategy myIndexDisablingStrategy;
        private readonly UnityExternalFilesFileSizeLogContributor myUsageStatistics;
        private readonly Dictionary<VirtualFileSystemPath, LifetimeDefinition> myRootPathLifetimes;
        private readonly VirtualFileSystemPath mySolutionDirectory;

        public UnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution,
                                                 ChangeManager changeManager,
                                                 IPsiModules psiModules,
                                                 PackageManager packageManager,
                                                 IShellLocks locks,
                                                 IFileSystemTracker fileSystemTracker,
                                                 UnityExternalPsiSourceFileFactory psiSourceFileFactory,
                                                 UnityExternalFilesModuleFactory moduleFactory,
                                                 UnityExternalFilesIndexDisablingStrategy indexDisablingStrategy,
                                                 UnityExternalFilesFileSizeLogContributor usageStatistics)
        {
            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myChangeManager = changeManager;
            myPackageManager = packageManager;
            myLocks = locks;
            myFileSystemTracker = fileSystemTracker;
            myPsiSourceFileFactory = psiSourceFileFactory;
            myModuleFactory = moduleFactory;
            myIndexDisablingStrategy = indexDisablingStrategy;
            myUsageStatistics = usageStatistics;

            myRootPathLifetimes = new Dictionary<VirtualFileSystemPath, LifetimeDefinition>();

            // SolutionDirectory isn't absolute in tests, and will throw an exception if we use it when we call Exists
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());

            changeManager.RegisterChangeProvider(lifetime, this);
            changeManager.AddDependency(lifetime, psiModules, this);
        }

        // Called once when we know it's a Unity solution. I.e. a solution that has a Unity reference (so can be true
        // for non-generated solutions)
        public virtual void OnHasUnityReference()
        {
            // For project model access
            myLocks.AssertReadAccessAllowed();

            var externalFiles = new ExternalFiles(mySolution, myLogger);
            CollectExternalFilesForSolutionDirectory(externalFiles, "Assets");
            CollectExternalFilesForSolutionDirectory(externalFiles, "ProjectSettings");
            CollectExternalFilesForPackages(externalFiles);

            // Disable asset indexing for massive projects. Note that we still collect all files, and always index
            // project settings, meta and asmdef files.
            myIndexDisablingStrategy.Run(externalFiles.AssetFiles);

            AddExternalFiles(externalFiles);
            UpdateStatistics(externalFiles);
            SubscribeToPackageUpdates();
            SubscribeToProjectModelUpdates();
        }

        public void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            // Do nothing. A project will either be in Assets or in a package, so either way, we've got it covered.
        }

        public void TryAddExternalPsiSourceFileForMiscFilesProjectFile(PsiModuleChangeBuilder builder,
                                                                       IProjectFile projectFile)
        {
            if (IsIndexedExternalFile(projectFile.Location) && GetExternalPsiSourceFile(projectFile.Location) == null)
            {
                var isUserEditable = IsUserEditable(projectFile.Location, out var isKnownExternalFile);
                if (!isKnownExternalFile)
                {
                    myLogger.Trace("Not creating PSI source file for {0}, which is external to the solution");
                    return;
                }

                Assertion.AssertNotNull(myModuleFactory.PsiModule, "myModuleFactory.PsiModule != null");

                // Create the source file and add it to the change builder. Normal files are explicitly added to the
                // module by FlushChanges, which doesn't get called in this scenario. Do it here.
                var sourceFile = AddExternalPsiSourceFile(builder, projectFile.Location, isUserEditable);
                myModuleFactory.PsiModule.Add(projectFile.Location, sourceFile, null);
            }
        }

        private bool IsUserEditable(VirtualFileSystemPath path, out bool isKnownExternalFile)
        {
            isKnownExternalFile = true;
            if (mySolutionDirectory.Combine("Assets").IsPrefixOf(path))
                return true;

            var packageData = myPackageManager.GetOwningPackage(path);
            if (packageData == null)
            {
                isKnownExternalFile = false;
                return false;
            }

            return packageData.IsUserEditable;
        }

        // This method is safe to call multiple times with the same folder (or sub folder)
        private void CollectExternalFilesForSolutionDirectory(ExternalFiles externalFiles, string relativePath)
        {
            var path = mySolutionDirectory.Combine(relativePath);
            if (path.ExistsDirectory)
                CollectExternalFilesForDirectory(externalFiles, path, true);
        }

        private void CollectExternalFilesForDirectory(ExternalFiles externalFiles, VirtualFileSystemPath directory,
                                                      bool isUserEditable)
        {
            Assertion.Assert(directory.IsAbsolute, "directory.IsAbsolute");

            // Don't process the entire solution directory - this would process Assets and Packages for a second time,
            // and also process Temp and Library, which are likely to be huge. This is unlikely, but be safe.
            if (directory == mySolutionDirectory)
            {
                myLogger.Error("Unexpected request to process entire solution directory. Skipping");
                return;
            }

            if (myRootPathLifetimes.ContainsKey(directory))
                return;

            // Make sure the directory hasn't already been processed as part of a previous directory. This shouldn't
            // happen, as we index based on folder or package, not project, so there is no way for us to see nested
            // folders
            foreach (var rootPath in myRootPathLifetimes.Keys)
            {
                if (rootPath.IsPrefixOf(directory))
                    return;
            }

            myLogger.Info("Processing directory for asset and meta files: {0}", directory);

            // Based on super simple tests, GetDirectoryEntries is faster than GetChildFiles with subsequent calls to
            // GetFileLength. But what is more surprising is that Windows in a VM is an order of magnitude FASTER than
            // Mac, on the same project!
            void CollectFiles(VirtualFileSystemPath path)
            {
                var entries = path.GetDirectoryEntries();
                foreach (var entry in entries)
                {
                    if (entry.IsDirectory)
                    {
                        // Do not add any directory tree that ends with `~`. Unity does not import these directories
                        // into the asset database
                        if (entry.RelativePath.Name.EndsWith("~"))
                            continue;
                        CollectFiles(entry.GetAbsolutePath());
                    }
                    else
                        externalFiles.ProcessExternalFile(entry, isUserEditable);
                }
            }

            CollectFiles(directory);
            externalFiles.AddDirectory(directory, isUserEditable);
            myRootPathLifetimes.Add(directory, myLifetime.CreateNested());
        }

        private void CollectExternalFilesForPackages(ExternalFiles externalFiles)
        {
            foreach (var (_ , packageData) in myPackageManager.Packages)
            {
                if (packageData.PackageFolder == null || packageData.PackageFolder.IsEmpty)
                    continue;

                // Index the whole of the package folder. All assets under a package are included into the Unity project
                // although only folders with a `.asmdef` will be treated as source and compiled into an assembly
                CollectExternalFilesForDirectory(externalFiles, packageData.PackageFolder, packageData.IsUserEditable);
            }
        }

        private void SubscribeToPackageUpdates()
        {
            // We've already processed all packages that were available when the project was first loaded, so this will
            // just be updating a single package at a time - Unity doesn't offer "update all".
            myPackageManager.Packages.AddRemove.Advise_NoAcknowledgement(myLifetime, args =>
            {
                var packageData = args.Value.Value;
                if (packageData.PackageFolder == null || packageData.PackageFolder.IsEmpty ||
                    myModuleFactory.PsiModule == null)
                {
                    return;
                }

                if (args.Action == AddRemove.Add)
                {
                    using (myLocks.UsingReadLock())
                    {
                        var externalFiles = new ExternalFiles(mySolution, myLogger);
                        CollectExternalFilesForDirectory(externalFiles, packageData.PackageFolder,
                            packageData.IsUserEditable);
                        AddExternalFiles(externalFiles);
                    }
                }
                else
                {
                    var psiModuleChanges = new PsiModuleChangeBuilder();
                    foreach (var sourceFile in myModuleFactory.PsiModule.GetSourceFilesByRootFolder(packageData.PackageFolder))
                        psiModuleChanges.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Removed);
                    FlushChanges(psiModuleChanges);

                    if (!myRootPathLifetimes.TryGetValue(packageData.PackageFolder, out var lifetimeDefinition))
                        myLogger.Warn("Cannot find lifetime for watched folder: {0}", packageData.PackageFolder);

                    lifetimeDefinition?.Terminate();
                    myRootPathLifetimes.Remove(packageData.PackageFolder);
                }
            });
        }

        private void SubscribeToProjectModelUpdates()
        {
            // If any of our external files are added to a proper project, remove them from our external files module.
            // Add them back if they're removed from the project model. This can happen if the user generates projects
            // for registry packages, or changes the settings to add .unity or .prefab to projects, or manually edits
            // the project to add the files (although this will get overwritten)
            myChangeManager.Changed2.Advise(myLifetime, args =>
            {
                var solutionChanges = args.ChangeMap.GetChanges<SolutionChange>().ToList();
                if (solutionChanges.IsEmpty())
                    return;

                var builder = new PsiModuleChangeBuilder();
                var visitor = new RecursiveProjectModelChangeDeltaVisitor(null, itemChange =>
                {
                    // Only handle changes to files in "real" projects - this means we need to remove our external file,
                    // as it's no longer external to the project. Don't process Misc Files project files - these are
                    // handled automatically by VS, or in response to adding/removing an external PSI file in Rider
                    // (which would lead to an infinite loop).
                    // Note that GetOldProject returns the project the file is being added to, or the project it has
                    // just been removed from
                    if (itemChange.ProjectItem is IProjectFile projectFile
                        && !itemChange.GetOldProject().IsMiscFilesProject()
                        && IsIndexedExternalFile(projectFile.Location))
                    {
                        if (itemChange.IsAdded || itemChange.IsMovedIn)
                        {
                            myLogger.Trace(
                                "External Unity file added to project {0}. Removing from external files module: {1}",
                                projectFile.GetProject()?.Name ?? "<null>", projectFile.Location);
                            RemoveExternalPsiSourceFile(builder, projectFile.Location);
                        }
                        else if ((itemChange.IsRemoved || itemChange.IsMovedOut) && itemChange.OldLocation.ExistsFile)
                        {
                            myLogger.Trace(
                                "External Unity file removed from project {0}. Adding to external files module: {1}",
                                itemChange.GetOldProject()?.Name ?? "<null>", itemChange.OldLocation);

                            var isUserEditable = IsUserEditable(itemChange.OldLocation, out var isKnownExternalFile);
                            if (isKnownExternalFile)
                                AddOrUpdateExternalPsiSourceFile(builder, itemChange.OldLocation, isUserEditable);
                        }
                    }
                });

                foreach (var solutionChange in solutionChanges)
                    solutionChange.Accept(visitor);

                if (!builder.IsEmpty)
                    myChangeManager.ExecuteAfterChange(() => FlushChanges(builder));
            });
        }

        private void AddExternalFiles([NotNull] ExternalFiles externalFiles)
        {
            var builder = new PsiModuleChangeBuilder();
            AddExternalPsiSourceFiles(externalFiles.MetaFiles, builder);
            AddExternalPsiSourceFiles(externalFiles.AssetFiles, builder);
            AddExternalPsiSourceFiles(externalFiles.AsmDefFiles, builder);
            FlushChanges(builder);

            foreach (var (path, isUserEditable) in externalFiles.Directories)
            {
                var lifetime = myRootPathLifetimes[path].Lifetime;
                myFileSystemTracker.AdviseDirectoryChanges(lifetime, path, true,
                    delta => OnWatchedDirectoryChange(delta, isUserEditable));
            }
        }

        private void AddExternalPsiSourceFiles(List<ExternalFile> files, PsiModuleChangeBuilder builder)
        {
            foreach (var file in files)
                AddOrUpdateExternalPsiSourceFile(builder, file.Path, file.IsUserEditable);
        }

        private void AddOrUpdateExternalPsiSourceFile(PsiModuleChangeBuilder builder, VirtualFileSystemPath path,
                                                      bool isUserEditable)
        {
            if (!UpdateExternalPsiSourceFile(builder, path))
                AddExternalPsiSourceFile(builder, path, isUserEditable);
        }

        private IPsiSourceFile AddExternalPsiSourceFile(PsiModuleChangeBuilder builder, VirtualFileSystemPath path,
                                                        bool isUserEditable)
        {
            // Daemon processes usually check IsGeneratedFile or IsNonUserFile before running. We treat assets as
            // generated, and asmdef files as not generated (yes they're generated by the UI, but we also expect the
            // user to edit them. We do not expect the user to edit assets). We also treat the asmdef file as a
            // non-user file if it's in a read only package. The daemon won't run here, which is helpful because
            // some of the built in packages have asmdefs that reference something that isn't another asmdef, e.g.
            // "Unity.Services.Core", "Windows.UI.Input.Spatial" or "Unity.InternalAPIEditorBridge.001". I don't
            // know what these are, and they're undocumented. But because the daemon doesn't run on readonly
            // packages, we don't show any resolve errors.
            // Note that we also want to treat assets as generated because otherwise, the to do manager will try to
            // parse all YAML files, which is Bad News for massive files.
            // TODO: Mark assets as non-user files, as they should not be edited manually
            // I'm not sure what this will affect
            var properties = path.IsAsmDef()
                ? new UnityExternalFileProperties(false, !isUserEditable)
                : new UnityExternalFileProperties(true, false);

            var sourceFile =
                myPsiSourceFileFactory.CreateExternalPsiSourceFile(myModuleFactory.PsiModule, path, properties);
            builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Added);

            return sourceFile;
        }

        private bool UpdateExternalPsiSourceFile(PsiModuleChangeBuilder builder, VirtualFileSystemPath path)
        {
            var sourceFile = GetExternalPsiSourceFile(path);
            if (sourceFile != null)
            {
                // Make sure we update the cached file system data, or all of the ICache implementations will think the
                // file is already up to date
                (sourceFile as PsiSourceFileFromPath)?.GetCachedFileSystemData().Refresh(path);
                builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Modified);
                return true;
            }

            return false;
        }

        private void RemoveExternalPsiSourceFile(PsiModuleChangeBuilder builder, VirtualFileSystemPath path)
        {
            var sourceFile = GetExternalPsiSourceFile(path);
            if (sourceFile != null)
                builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Removed);
        }

        private void UpdateStatistics(ExternalFiles externalFiles)
        {
            foreach (var externalFile in externalFiles.AssetFiles)
            {
                UnityExternalFilesFileSizeLogContributor.FileType? fileType = null;
                if (externalFile.Path.IsAsset())
                    fileType = UnityExternalFilesFileSizeLogContributor.FileType.Asset;
                else if (externalFile.Path.IsPrefab())
                    fileType = UnityExternalFilesFileSizeLogContributor.FileType.Prefab;
                else if (externalFile.Path.IsScene())
                    fileType = UnityExternalFilesFileSizeLogContributor.FileType.Scene;

                if (fileType.HasValue)
                    myUsageStatistics.AddStatistic(fileType.Value, externalFile.Length, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.MetaFiles)
            {
                myUsageStatistics.AddStatistic(UnityExternalFilesFileSizeLogContributor.FileType.Meta,
                    externalFile.Length, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.AsmDefFiles)
            {
                myUsageStatistics.AddStatistic(UnityExternalFilesFileSizeLogContributor.FileType.AsmDef,
                    externalFile.Length, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.KnownBinaryAssetFiles)
            {
                myUsageStatistics.AddStatistic(UnityExternalFilesFileSizeLogContributor.FileType.KnownBinary,
                    externalFile.Length, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.ExcludedByNameAssetFiles)
            {
                myUsageStatistics.AddStatistic(UnityExternalFilesFileSizeLogContributor.FileType.ExcludedByName,
                    externalFile.Length, externalFile.IsUserEditable);
            }
        }

        private static bool IsIndexedExternalFile(VirtualFileSystemPath path)
        {
            return path.IsIndexedExternalFile() && !IsBinaryAsset(path) && !IsAssetExcludedByName(path);
        }

        private static bool IsBinaryAsset(VirtualDirectoryEntryData directoryEntry)
        {
            if (IsKnownBinaryAssetByName(directoryEntry.RelativePath))
                return true;

            return directoryEntry.Length > AssetFileCheckSizeThreshold && directoryEntry.RelativePath.IsAsset() &&
                   !directoryEntry.GetAbsolutePath().SniffYamlHeader();
        }

        private static bool IsBinaryAsset(VirtualFileSystemPath path)
        {
            if (IsKnownBinaryAssetByName(path))
                return true;

            if (!path.ExistsFile)
                return false;

            var fileLength = (ulong) path.GetFileLength();
            return fileLength > AssetFileCheckSizeThreshold && path.IsAsset() && !path.SniffYamlHeader();
        }

        private static bool IsKnownBinaryAssetByName(IPath path)
        {
            // Even if the project is set to ForceText, some files will always be binary, notably LightingData.asset.
            // Users can also force assets to serialise as binary with the [PreferBinarySerialization] attribute
            return path.Name.Equals("LightingData.asset", StringComparison.InvariantCultureIgnoreCase);
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

        private void OnWatchedDirectoryChange(FileSystemChangeDelta delta, bool isDirectoryUserEditable)
        {
            myLocks.ExecuteOrQueue(Lifetime.Eternal, "UnityExternalFilesModuleProcessor::OnWatchedDirectoryChange",
                () =>
                {
                    using (ReadLockCookie.Create())
                    {
                        var builder = new PsiModuleChangeBuilder();
                        ProcessFileSystemChangeDelta(delta, builder, isDirectoryUserEditable);
                        FlushChanges(builder);
                    }
                });
        }

        private void ProcessFileSystemChangeDelta(FileSystemChangeDelta delta, PsiModuleChangeBuilder builder,
                                                  bool isDirectoryUserEditable)
        {
            // For project model access
            myLocks.AssertReadAccessAllowed();

            var module = myModuleFactory.PsiModule;
            if (module == null)
                return;

            // Note that we watch for changes in all folders in a Unity solution - Assets and packages, wherever they
            // are - Packages, Library/PackageCache, file:, etc. Any package folder might contain projects, even read
            // only packages, if project generation is enabled. Therefore any file might be in a project. Check when
            // adding, but we don't need to for update and remove as they will only work on known files.
            switch (delta.ChangeType)
            {
                // We can get ADDED for a file we already know about if an app saves the file by saving to a temp file
                // first. We don't get a DELETED first, surprisingly. Treat this scenario like CHANGED
                case FileSystemChangeType.ADDED:
                    if (IsIndexedExternalFile(delta.NewPath) &&
                        !mySolution.FindProjectItemsByLocation(delta.NewPath).Any())
                    {
                        AddOrUpdateExternalPsiSourceFile(builder, delta.NewPath, isDirectoryUserEditable);
                    }
                    break;

                case FileSystemChangeType.DELETED:
                    RemoveExternalPsiSourceFile(builder, delta.OldPath);
                    break;

                // We can get RENAMED if an app saves the file by saving to a temporary name first, then renaming
                case FileSystemChangeType.CHANGED:
                case FileSystemChangeType.RENAMED:
                    UpdateExternalPsiSourceFile(builder, delta.NewPath);
                    break;

                case FileSystemChangeType.SUBTREE_CHANGED:
                case FileSystemChangeType.UNKNOWN:
                    break;
            }

            foreach (var child in delta.GetChildren())
                ProcessFileSystemChangeDelta(child, builder, isDirectoryUserEditable);
        }

        [CanBeNull]
        private IPsiSourceFile GetExternalPsiSourceFile(VirtualFileSystemPath path)
        {
            Assertion.AssertNotNull(myModuleFactory.PsiModule, "myModuleFactory.PsiModule != null");
            return myModuleFactory.PsiModule.TryGetFileByPath(path, out var sourceFile) ? sourceFile : null;
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

        public struct ExternalFile
        {
            public readonly VirtualFileSystemPath Path;
            public readonly ulong Length;
            public readonly bool IsUserEditable;

            public ExternalFile(VirtualDirectoryEntryData directoryEntry, bool isUserEditable)
            {
                Path = directoryEntry.GetAbsolutePath();
                Length = directoryEntry.Length;
                IsUserEditable = isUserEditable;
            }
        }

        private class ExternalFiles
        {
            private readonly ISolution mySolution;
            private readonly ILogger myLogger;
            public readonly List<ExternalFile> MetaFiles = new();
            public readonly List<ExternalFile> AssetFiles = new();
            public readonly List<ExternalFile> AsmDefFiles = new();
            public FrugalLocalList<ExternalFile> KnownBinaryAssetFiles;
            public FrugalLocalList<ExternalFile> ExcludedByNameAssetFiles;
            public FrugalLocalList<(VirtualFileSystemPath directory, bool isUserEditable)> Directories;

            public ExternalFiles(ISolution solution, ILogger logger)
            {
                mySolution = solution;
                myLogger = logger;
            }

            public void ProcessExternalFile(VirtualDirectoryEntryData directoryEntry, bool isUserEditable)
            {
                mySolution.Locks.AssertReadAccessAllowed();

                if (directoryEntry.RelativePath.IsMeta())
                    MetaFiles.Add(new ExternalFile(directoryEntry, isUserEditable));
                else if (directoryEntry.RelativePath.IsYamlDataFile())
                {
                    if (IsBinaryAsset(directoryEntry))
                        KnownBinaryAssetFiles.Add(new ExternalFile(directoryEntry, isUserEditable));
                    else if (IsAssetExcludedByName(directoryEntry.RelativePath))
                        ExcludedByNameAssetFiles.Add(new ExternalFile(directoryEntry, isUserEditable));
                    else
                        AssetFiles.Add(new ExternalFile(directoryEntry, isUserEditable));
                }
                else if (directoryEntry.RelativePath.IsAsmDef())
                {
                    // Do not add if this file is already part of a project. This might be because it's from an editable
                    // package, or because the user has package project generation enabled.
                    // It might be part of the Misc Files project (not sure why - perhaps cached, perhaps because the
                    // file was already open at startup), in which case, add a PsiSourceFile if it doesn't already exist
                    // These checks require a read lock!
                    var existingProjectFile = mySolution.FindProjectItemsByLocation(directoryEntry.GetAbsolutePath())
                        .FirstOrDefault() as IProjectFile;
                    var existingPsiSourceFile = existingProjectFile?.ToSourceFile();
                    if (existingPsiSourceFile != null)
                    {
                        myLogger.Warn("Found existing project file for {0} with existing PSI source file (IsMiscProjectItem: {1})",
                            directoryEntry.GetAbsolutePath(), existingProjectFile.IsMiscProjectItem());
                    }
                    else
                    {
                        AsmDefFiles.Add(new ExternalFile(directoryEntry, isUserEditable));
                    }
                }
            }

            public void AddDirectory(VirtualFileSystemPath directory, bool isUserEditable)
            {
                Directories.Add((directory, isUserEditable));
            }
        }
    }
}