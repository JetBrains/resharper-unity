using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.SolutionAnalysis.FileImages;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Services.UsageStatistics;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.InputActions.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.dataStructures;
using JetBrains.Util.Logging;
using ProjectExtensions = JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ProjectExtensions;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class UnityExternalFilesModuleProcessor : IChangeProvider, IUnityReferenceChangeHandler
    {
        private const long AssetFileCheckSizeThreshold = 20 * (1024 * 1024); // 20 MB

        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly ChangeManager myChangeManager;
        private readonly PackageManager myPackageManager;
        private readonly IShellLocks myLocks;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly IProjectFileExtensions myProjectFileExtensions;
        private readonly UnityExternalPsiSourceFileFactory myPsiSourceFileFactory;
        private readonly UnityExternalFilesModuleFactory myModuleFactory;
        private readonly UnityExternalFilesIndexDisablingStrategy myIndexDisablingStrategy;
        private readonly UnityAssetInfoCollector myUsageStatistics;
        private readonly AssetIndexingSupport myAssetIndexingSupport;
        private readonly Dictionary<VirtualFileSystemPath, LifetimeDefinition> myRootPathLifetimes;
        private readonly VirtualFileSystemPath mySolutionDirectory;
        private readonly VirtualFileSystemPath myProjectSettingsFolder;
        private readonly UnityExternalProjectFileTypes myExternalProjectFileTypes;

        public UnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution,
                                                 ChangeManager changeManager,
                                                 IPsiModules psiModules,
                                                 PackageManager packageManager,
                                                 IShellLocks locks,
                                                 IFileSystemTracker fileSystemTracker,
                                                 IProjectFileExtensions projectFileExtensions,
                                                 UnityExternalPsiSourceFileFactory psiSourceFileFactory,
                                                 UnityExternalFilesModuleFactory moduleFactory,
                                                 UnityExternalFilesIndexDisablingStrategy indexDisablingStrategy,
                                                 UnityAssetInfoCollector usageStatistics,
                                                 AssetIndexingSupport assetIndexingSupport, UnityExternalProjectFileTypes externalProjectFileTypes)
        {
            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myChangeManager = changeManager;
            myPackageManager = packageManager;
            myLocks = locks;
            myFileSystemTracker = fileSystemTracker;
            myProjectFileExtensions = projectFileExtensions;
            myPsiSourceFileFactory = psiSourceFileFactory;
            myModuleFactory = moduleFactory;
            myIndexDisablingStrategy = indexDisablingStrategy;
            myUsageStatistics = usageStatistics;
            myAssetIndexingSupport = assetIndexingSupport;
            myExternalProjectFileTypes = externalProjectFileTypes;

            myRootPathLifetimes = new Dictionary<VirtualFileSystemPath, LifetimeDefinition>();

            // SolutionDirectory isn't absolute in tests, and will throw an exception if we use it when we call Exists
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());

            myProjectSettingsFolder = mySolutionDirectory.Combine(ProjectExtensions.ProjectSettingsFolder);
            
            changeManager.RegisterChangeProvider(lifetime, this);
            changeManager.AddDependency(lifetime, psiModules, this);

            assetIndexingSupport.IsEnabled.Change.Advise(lifetime, args =>
            {
                // previously disabled, now enabled
                if (args.HasOld && !args.Old && args.HasNew && args.New)
                {
                    myLocks.ExecuteOrQueueReadLockEx(lifetime, "UnityInitialUpdateExternalFiles", () =>
                    {
                        CollectInitialFiles(false);
                    });
                }
            });
        }

        private bool IsIndexedWithCurrentIndexingSupport(VirtualFileSystemPath path)
        {
            if (myAssetIndexingSupport.IsEnabled.Value)
                return myExternalProjectFileTypes.ShouldBeIndexed(path, true);

            return IsIndexedFileWithDisabledAssetSupport(path);
        }

        private bool IsIndexedFileWithDisabledAssetSupport(VirtualFileSystemPath path) => 
            myExternalProjectFileTypes.ShouldBeIndexed(path, false) || path.IsAsmDefMeta() /* HACK, normally .meta files excluded */ || IsFromProjectSettingsFolder(path) || path.IsFromResourceFolder();

        private bool IsFromProjectSettingsFolder(VirtualFileSystemPath path) => path.StartsWith(myProjectSettingsFolder);

        private ExternalFiles FilterFiles(ExternalFiles files)
        {
            if (myAssetIndexingSupport.IsEnabled.Value)
                return files;

            var newFiles = new ExternalFiles(mySolution, myExternalProjectFileTypes, myLogger);

            FilterFiles(files.MetaFiles, newFiles.MetaFiles);
            FilterFiles(files.AssetFiles, newFiles.AssetFiles);
            FilterFiles(files.IndexableFiles, newFiles.IndexableFiles);

            newFiles.Directories.AddRange(files.Directories);
            
            return newFiles;
        }

        private void FilterFiles(List<ExternalFile> files, List<ExternalFile> newFiles)
        {
            foreach (var metaFile in files)
            {
                var path = metaFile.Path;
                if (IsIndexedFileWithDisabledAssetSupport(path))
                {
                    newFiles.Add(metaFile);
                }
            }
        }


        // TODO: This is all run on the main thread, at least during solution load, which is very expensive
        // Are the PSI caches loaded before or after this? If we move collection to a background thread, would we clean
        // up "stale" PSI files from the caches because they haven't been found yet?

        // Called once when we know it's a Unity solution. I.e. a solution that has a Unity reference (so can be true
        // for non-generated solutions)
        public virtual void OnHasUnityReference()
        {
            // For project model access
            myLocks.AssertReadAccessAllowed();

            var externalFiles = CollectInitialFiles(true);

            try
            {
                UpdateStatistics(externalFiles);
                externalFiles.Dump();
            }
            catch (Exception e)
            {
                myLogger.Error(e);
            }

            SubscribeToPackageUpdates();
            SubscribeToProjectModelUpdates();
            
            myUsageStatistics.FinishInitialUpdate();
        }

        private ExternalFiles CollectInitialFiles(bool initialRun)
        {
            var externalFiles = myLogger.DoCalculation("CollectExternalFiles", null,
                () =>
                {
                    var roots = myRootPathLifetimes.Keys.ToList();

                    foreach (var root in roots)
                    {
                        myRootPathLifetimes[root].Terminate();
                        myRootPathLifetimes.Remove(root);
                    }
                    
                    var files = new ExternalFiles(mySolution, myExternalProjectFileTypes, myLogger);
                    CollectExternalFilesForSolutionDirectory(files, "Assets");
                    CollectExternalFilesForSolutionDirectory(files, "ProjectSettings", true);
                    CollectExternalFilesForPackages(files);

                    // Disable asset indexing for massive projects. Note that we still collect all files, and always index
                    // project settings, meta and asmdef files.

                    if (initialRun)
                    {
                        myIndexDisablingStrategy.Run(files.AssetFiles);
                    }

                    return FilterFiles(files);
                });

            myLogger.DoActivity("ProcessExternalFiles", null,
                () => AddExternalFiles(externalFiles));
            return externalFiles;
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

                Assertion.AssertNotNull(myModuleFactory.PsiModule);

                // Create the source file and add it to the change builder. In a bulk scenario, we use the contents of
                // the builder to FlushChanges and update the module, but this isn't our builder, so update the module
                // directly. Creating a new instance of CachedFileSystemData will hit the disk, but that's ok for a
                // single file.
                var fileSystemData = new CachedFileSystemData(projectFile.Location);
                var sourceFile = AddExternalPsiSourceFile(builder, projectFile.Location, projectFile.LanguageType,
                    fileSystemData, isUserEditable);
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
        private void CollectExternalFilesForSolutionDirectory(ExternalFiles externalFiles, string relativePath,
                                                              bool isProjectSettingsFolder = false)
        {
            var path = mySolutionDirectory.Combine(relativePath);
            if (path.ExistsDirectory)
                CollectExternalFilesForDirectory(externalFiles, path, true, isProjectSettingsFolder);
        }

        private void CollectExternalFilesForDirectory(ExternalFiles externalFiles, VirtualFileSystemPath directory,
                                                      bool isUserEditable, bool isProjectSettingsFolder = false)
        {
            Assertion.Assert(directory.IsAbsolute);

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

            myLogger.DoActivity("CollectExternalFilesForDirectory", directory.FullPath, () =>
            {
                CollectFiles(directory, externalFiles, isProjectSettingsFolder);
                externalFiles.AddDirectory(directory, isUserEditable);
                myRootPathLifetimes.Add(directory, myLifetime.CreateNested());
            });

            // Based on super simple tests, GetDirectoryEntries is faster than GetChildFiles with subsequent calls to
            // GetFileLength. But what is more surprising is that Windows in a VM is an order of magnitude FASTER than
            // Mac, on the same project!
            void CollectFiles(VirtualFileSystemPath path, ExternalFiles files, bool isProjectSettings)
            {
                var entries = path.GetDirectoryEntries();
                foreach (var entry in entries)
                {
                    if (entry.IsDirectory)
                    {
                        // Do not add any directory tree that ends with `~` or starts with `.`.
                        // Unity does not import these directories into the asset database
                        if (IsHiddenAssetFolder(entry))
                            continue;
                        var entryAbsolutePath = entry.GetAbsolutePath();
                        myLogger.Trace($"Processing directory {entryAbsolutePath}");
                        CollectFiles(entryAbsolutePath, files, isProjectSettings);
                    }
                    else
                        files.ProcessExternalFile(entry, isUserEditable, isProjectSettings);
                }
            }
        }

        private static bool IsHiddenAssetFolder(VirtualDirectoryEntryData entry)
        {
            return entry.RelativePath.FullPath.EndsWith("~") || entry.RelativePath.FullPath.StartsWith(".");
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
                if (packageData.PackageFolder == null || packageData.PackageFolder.IsEmpty)
                    return;

                myLogger.Verbose($"PackageUpdates {packageData.PackageFolder} {args.Action}");

                if (args.Action == AddRemove.Add)
                {
                    using (myLocks.UsingReadLock())
                    {
                        var externalFiles = new ExternalFiles(mySolution, myExternalProjectFileTypes, myLogger);
                        CollectExternalFilesForDirectory(externalFiles, packageData.PackageFolder,
                            packageData.IsUserEditable);
                        AddExternalFiles(FilterFiles(externalFiles));
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
            myChangeManager.Changed.Advise(myLifetime, args =>
            {
                var solutionChanges = args.ChangeMap.GetChanges<SolutionChange>().ToList();
                if (solutionChanges.IsEmpty())
                    return;

                var processedFiles = new HashSet<VirtualFileSystemPath>(); // avoid DEXP-730021 SourceFile is not valid.
                var builder = new PsiModuleChangeBuilder();
                var visitor = new RecursiveProjectModelChangeDeltaVisitor(null, itemChange =>
                {
                    // Only handle changes to files in "real" projects - this means we need to remove our external file,
                    // as it's no longer external to the project. Don't process Misc Files project files - these are
                    // handled automatically by VS, or in response to adding/removing an external PSI file in Rider
                    // (which would lead to an infinite loop).
                    // Note that GetOldProject returns the project the file is being added to, or the project it has
                    // just been removed from
                    // Warning: Removing .Player project should not cause adding file to the ExternalModule 
                    if (itemChange.ProjectItem is IProjectFile projectFile
                        && IsIndexedExternalFile(projectFile.Location))
                    {
                        if ((itemChange.IsAdded || itemChange.IsMovedIn) && !itemChange.ProjectItem.IsMiscProjectItem())
                        {
                            myLogger.Trace(
                                "External Unity file added to project {0}. Removing from external files module: {1}",
                                projectFile.GetProject()?.Name ?? "<null>", projectFile.Location);

                            RemoveExternalPsiSourceFile(builder, projectFile.Location);
                        }
                        else if ((itemChange.IsRemoved || itemChange.IsMovedOut) && itemChange.OldLocation.ExistsFile 
                                 && mySolution.FindProjectItemsByLocation(itemChange.OldLocation).All(t => t.IsMiscProjectItem()))
                        {
                            var isUserEditable = IsUserEditable(itemChange.OldLocation, out var isKnownExternalFile);
                            if (isKnownExternalFile && !processedFiles.Contains(itemChange.OldLocation))
                            {
                                myLogger.Trace(
                                    "External Unity file removed from project {0}. Adding to external files module: {1}",
                                    itemChange.GetOldProject()?.Name ?? "<null>", itemChange.OldLocation);
                                
                                processedFiles.Add(itemChange.OldLocation);
                                AddOrUpdateExternalPsiSourceFile(builder, itemChange.OldLocation,
                                    myProjectFileExtensions.GetFileType(projectFile.Location), isUserEditable);
                            }
                        }
                    }
                });

                foreach (var solutionChange in solutionChanges)
                    solutionChange.Accept(visitor);

                if (!builder.IsEmpty)
                    myChangeManager.ExecuteAfterChange(() => FlushChanges(builder));
            });
        }

        private void AddExternalFiles(ExternalFiles externalFiles)
        {
            var builder = new PsiModuleChangeBuilder();
            AddExternalPsiSourceFiles(externalFiles.MetaFiles, builder, "meta");
            AddExternalPsiSourceFiles(externalFiles.AssetFiles, builder, "asset");
            AddExternalPsiSourceFiles(externalFiles.IndexableFiles, builder, "other");
            FlushChanges(builder);

            foreach (var (path, isUserEditable) in externalFiles.Directories)
            {
                var lifetime = myRootPathLifetimes[path].Lifetime;
                myFileSystemTracker.AdviseDirectoryChanges(lifetime, path, true,
                    delta => OnWatchedDirectoryChange(delta, isUserEditable));
            }
        }

        private void AddExternalPsiSourceFiles(List<ExternalFile> files, PsiModuleChangeBuilder builder, string kind)
        {
            myLogger.Verbose("Adding/updating PSI source files for {0} {1} external files", files.Count, kind);

            foreach (var file in files)
            {
                AddOrUpdateExternalPsiSourceFile(builder, file.Path, file.ProjectFileType, file.IsUserEditable,
                    file.FileSystemData);
            }
        }

        // Note that it is better to pass null for fileSystemData than to create your own instance. If we're updating,
        // we'll refresh in place. If we're adding a new file, we'll create fileSystemData if it's missing
        private void AddOrUpdateExternalPsiSourceFile(PsiModuleChangeBuilder builder,
                                                      VirtualFileSystemPath path,
                                                      ProjectFileType projectFileType,
                                                      bool isUserEditable,
                                                      CachedFileSystemData? fileSystemData = null)
        {
            if (!UpdateExternalPsiSourceFile(builder, path, fileSystemData))
            {
                fileSystemData ??= new CachedFileSystemData(path);
                AddExternalPsiSourceFile(builder, path, projectFileType, fileSystemData, isUserEditable);
            }
        }

        private IPsiSourceFile AddExternalPsiSourceFile(PsiModuleChangeBuilder builder,
                                                        VirtualFileSystemPath path,
                                                        ProjectFileType projectFileType,
                                                        CachedFileSystemData fileSystemData,
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
            var properties = myExternalProjectFileTypes.ShouldBeTreatedAsNonGenerated(path)
                ? new UnityExternalFileProperties(false, !isUserEditable)
                : new UnityExternalFileProperties(true, false);

            var sourceFile = myPsiSourceFileFactory.CreateExternalPsiSourceFile(myModuleFactory.PsiModule, path,
                projectFileType, properties, fileSystemData);
            
            if(path.IsMeta() && path.IsFromResourceFolder())
                sourceFile.PutData(FileImagesBuilder.FileImagesBuilderAllowKey, FileImagesBuilderAllowKey.Instance);

            builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Added);

            return sourceFile;
        }

        private bool UpdateExternalPsiSourceFile(PsiModuleChangeBuilder builder,
                                                 VirtualFileSystemPath path,
                                                 CachedFileSystemData? fileSystemData = null)
        {
            var sourceFile = GetExternalPsiSourceFile(path);
            if (sourceFile != null)
            {
                if (sourceFile is IPsiSourceFileWithLocation sourceFileWithLocation)
                {
                    // Make sure we update the cached file system data, or all of the ICache implementations will think
                    // the file is already up to date. Refreshing the existing file system data will hit the disk, so
                    // avoid it if the data is already available. If the data is not available, pass null, and we'll
                    // refresh the existing data in place without another allocation
                    var existingFileSystemData = sourceFileWithLocation.GetCachedFileSystemData();
                    if (fileSystemData != null)
                    {
                        existingFileSystemData.FileAttributes = fileSystemData.FileAttributes;
                        existingFileSystemData.FileExists = fileSystemData.FileExists;
                        existingFileSystemData.FileLength = fileSystemData.FileLength;
                        existingFileSystemData.LastWriteTimeUtc = fileSystemData.LastWriteTimeUtc;
                    }
                    else
                        existingFileSystemData.Load(path);
                }

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
                UnityAssetInfoCollector.FileType? fileType = null;
                if (externalFile.Path.IsAsset())
                    fileType = UnityAssetInfoCollector.FileType.Asset;
                else if (externalFile.Path.IsPrefab())
                    fileType = UnityAssetInfoCollector.FileType.Prefab;
                else if (externalFile.Path.IsScene())
                    fileType = UnityAssetInfoCollector.FileType.Scene;
                else if (externalFile.Path.IsAnim())
                    fileType = UnityAssetInfoCollector.FileType.Anim;
                else if (externalFile.Path.IsController())
                    fileType = UnityAssetInfoCollector.FileType.Controller;

                if (fileType.HasValue)
                {
                    myUsageStatistics.AddStatistic(fileType.Value, externalFile.FileSystemData.FileLength,
                        externalFile.IsUserEditable);
                }
            }

            foreach (var externalFile in externalFiles.MetaFiles)
            {
                myUsageStatistics.AddStatistic(UnityAssetInfoCollector.FileType.Meta,
                    externalFile.FileSystemData.FileLength, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.IndexableFiles)
            {
                UnityAssetInfoCollector.FileType fileType;
                if (ReferenceEquals(externalFile.ProjectFileType, AsmDefProjectFileType.Instance))
                    fileType = UnityAssetInfoCollector.FileType.AsmDef;
                else if (ReferenceEquals(externalFile.ProjectFileType, AsmRefProjectFileType.Instance))
                    fileType = UnityAssetInfoCollector.FileType.AsmRef;
                else if (ReferenceEquals(externalFile.ProjectFileType, InputActionsProjectFileType.Instance))
                    fileType = UnityAssetInfoCollector.FileType.InputActions;
                else
                    continue;
                myUsageStatistics.AddStatistic(fileType, externalFile.FileSystemData.FileLength, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.KnownBinaryAssetFiles)
            {
                myUsageStatistics.AddStatistic(UnityAssetInfoCollector.FileType.KnownBinary,
                    externalFile.FileSystemData.FileLength, externalFile.IsUserEditable);
            }

            foreach (var externalFile in externalFiles.ExcludedByNameAssetFiles)
            {
                myUsageStatistics.AddStatistic(UnityAssetInfoCollector.FileType.ExcludedByName,
                    externalFile.FileSystemData.FileLength, externalFile.IsUserEditable);
            }
        }
        
        private bool IsIndexedExternalFile(VirtualFileSystemPath path) => IsIndexedWithCurrentIndexingSupport(path) && !IsBinaryAsset(path) && !IsAssetExcludedByName(path);

        private static bool IsBinaryAsset(VirtualDirectoryEntryData directoryEntry)
        {
            if (IsKnownBinaryAssetByName(directoryEntry.RelativePath))
                return true;

            // If a .asset file is over a certain size, sniff the header to see if it's binary or YAML. Otherwise, we
            // treat the files as text
            return directoryEntry.Length > AssetFileCheckSizeThreshold && directoryEntry.RelativePath.IsAsset() &&
                   !directoryEntry.GetAbsolutePath().SniffYamlHeader();
        }

        private static bool IsBinaryAsset(VirtualFileSystemPath path)
        {
            if (IsKnownBinaryAssetByName(path))
                return true;

            if (!path.ExistsFile)
                return false;

            // If a .asset file is over a certain size, sniff the header to see if it's binary or YAML. Otherwise, we
            // treat the files as text
            var fileLength = path.GetFileLength();
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
                        // Note that ExtensionWithDot allocates, which can hurt if we have to process thousands of files
                        // We should be safe here - we'll only receive this event for watched folders, so we don't
                        // expect thousands of files to suddenly appear. A new package might introduce that many files,
                        // but new packages are handled with a separate notification
                        var projectFileType = myProjectFileExtensions.GetFileType(delta.NewPath.ExtensionWithDot);
                        AddOrUpdateExternalPsiSourceFile(builder, delta.NewPath, projectFileType, isDirectoryUserEditable);
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

        private IPsiSourceFile? GetExternalPsiSourceFile(VirtualFileSystemPath path)
        {
            Assertion.AssertNotNull(myModuleFactory.PsiModule);
            return myModuleFactory.PsiModule.TryGetFileByPath(path, out var sourceFile) ? sourceFile : null;
        }

        private void FlushChanges(PsiModuleChangeBuilder builder)
        {
            myLocks.ReentrancyGuard.AssertGuarded();

            if (builder.IsEmpty)
                return;

            var module = myModuleFactory.PsiModule;
            Assertion.AssertNotNull(module);
            myLocks.AssertMainThread();
            using (myLocks.UsingWriteLock())
            {
                var psiModuleChange = builder.Result;

                myLogger.Verbose("Flushing {0} PSI source file changes", psiModuleChange.FileChanges.Count);
                if (myLogger.IsTraceEnabled())
                {
                    myLogger.Verbose("{0} added, {1} removed, {2} modified, {3} invalidated",
                        psiModuleChange.FileChanges.Count(c => c.Type == PsiModuleChange.ChangeType.Added),
                        psiModuleChange.FileChanges.Count(c => c.Type == PsiModuleChange.ChangeType.Removed),
                        psiModuleChange.FileChanges.Count(c => c.Type == PsiModuleChange.ChangeType.Modified),
                        psiModuleChange.FileChanges.Count(c => c.Type == PsiModuleChange.ChangeType.Invalidated));
                }

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

                myLogger.DoActivity("FlushChanges::OnProviderChanged", null, () =>
                    myChangeManager.OnProviderChanged(this, psiModuleChange, SimpleTaskExecutor.Instance));
            }
        }

        public object? Execute(IChangeMap changeMap) => null;

        public struct ExternalFile
        {
            public readonly VirtualFileSystemPath Path;
            public readonly CachedFileSystemData FileSystemData;
            public readonly ProjectFileType ProjectFileType;
            public readonly bool IsUserEditable;

            public ExternalFile(VirtualDirectoryEntryData directoryEntry, ProjectFileType? projectFileType,
                                bool isUserEditable)
            {
                Path = directoryEntry.GetAbsolutePath();
                FileSystemData = new CachedFileSystemData(directoryEntry);
                ProjectFileType = projectFileType.NotNull("ProjectFileType != null");
                IsUserEditable = isUserEditable;
            }
        }

        private class ExternalFiles
        {
            private readonly ISolution mySolution;
            private readonly UnityExternalProjectFileTypes myProjectFileTypes;
            private readonly ILogger myLogger;
            public readonly List<ExternalFile> MetaFiles = new();
            public readonly List<ExternalFile> AssetFiles = new();
            public readonly List<ExternalFile> IndexableFiles = new();
            public FrugalLocalList<ExternalFile> KnownBinaryAssetFiles;
            public FrugalLocalList<ExternalFile> ExcludedByNameAssetFiles;
            public FrugalLocalList<(VirtualFileSystemPath directory, bool isUserEditable)> Directories;

            public ExternalFiles(ISolution solution, UnityExternalProjectFileTypes projectFileTypes, ILogger logger)
            {
                mySolution = solution;
                myProjectFileTypes = projectFileTypes;
                myLogger = logger;
            }

            public void ProcessExternalFile(VirtualDirectoryEntryData directoryEntry,
                                            bool isUserEditable, bool isProjectSettingsAsset)
            {
                mySolution.Locks.AssertReadAccessAllowed();

                if (directoryEntry.RelativePath.IsMeta())
                    MetaFiles.Add(new ExternalFile(directoryEntry, MetaProjectFileType.Instance, isUserEditable));
                else if (directoryEntry.RelativePath.IsYamlDataFile())
                {
                    ProjectFileType? projectFileType = isProjectSettingsAsset
                        ? YamlProjectFileType.Instance
                        : UnityYamlProjectFileType.Instance;
                    if (IsBinaryAsset(directoryEntry))
                        KnownBinaryAssetFiles.Add(new ExternalFile(directoryEntry, projectFileType, isUserEditable));
                    else if (IsAssetExcludedByName(directoryEntry.RelativePath))
                        ExcludedByNameAssetFiles.Add(new ExternalFile(directoryEntry, projectFileType, isUserEditable));
                    else
                        AssetFiles.Add(new ExternalFile(directoryEntry, projectFileType, isUserEditable));
                }
                else if (myProjectFileTypes.TryGetFileInfo(directoryEntry.RelativePath, out var info))
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
                        IndexableFiles.Add(new ExternalFile(directoryEntry, info.ProjectFileType, isUserEditable));
                    }
                }
            }

            public void AddDirectory(VirtualFileSystemPath directory, bool isUserEditable)
            {
                Directories.Add((directory, isUserEditable));
            }

            public void Dump()
            {
                if (!myLogger.IsTraceEnabled()) return;

                var total = MetaFiles.Count + AssetFiles.Count + IndexableFiles.Count +
                            KnownBinaryAssetFiles.Count + ExcludedByNameAssetFiles.Count;
                myLogger.Trace("Collected {0} external files", total);
                myLogger.Trace("Meta files: {0} ({1:n0} bytes)", MetaFiles.Count, GetTotalFileSize(MetaFiles));
                myLogger.Trace("Asset files: {0} ({1:n0} bytes)", AssetFiles.Count, GetTotalFileSize(AssetFiles));
                myLogger.Trace("Other indexable files: {0} ({1:n0} bytes)", IndexableFiles.Count, GetTotalFileSize(IndexableFiles));
                myLogger.Trace("Known binary asset files: {0} ({1:n0} bytes)", KnownBinaryAssetFiles.Count, GetTotalFileSize(KnownBinaryAssetFiles.AsIReadOnlyList()));
                myLogger.Trace("Excluded by name files: {0} ({1:n0} bytes)", ExcludedByNameAssetFiles.Count, GetTotalFileSize(ExcludedByNameAssetFiles.AsIReadOnlyList()));
                myLogger.Trace("Directories: {0}", Directories.Count);
            }

            private static ulong GetTotalFileSize(IEnumerable<ExternalFile> files) =>
                files.Aggregate(0UL, (s, f) => s + (ulong) f.FileSystemData.FileLength);
        }
    }
}