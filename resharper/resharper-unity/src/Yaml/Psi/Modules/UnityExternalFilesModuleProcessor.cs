using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Common;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ProjectModel.Transaction;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    [SolutionComponent]
    public class UnityExternalFilesModuleProcessor : IChangeProvider, IUnityReferenceChangeHandler
    {
        // stats
        public readonly List<ulong> PrefabSizes = new List<ulong>();
        public readonly List<ulong> SceneSizes = new List<ulong>();
        public readonly List<ulong> AssetSizes = new List<ulong>();

        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly ChangeManager myChangeManager;
        private readonly IShellLocks myLocks;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly ProjectFilePropertiesFactory myProjectFilePropertiesFactory;
        private readonly UnityYamlPsiSourceFileFactory myPsiSourceFileFactory;
        private readonly UnityExternalFilesModuleFactory myModuleFactory;
        private readonly UnityYamlDisableStrategy myUnityYamlDisableStrategy;
        private readonly AssetSerializationMode myAssetSerializationMode;
        private readonly JetHashSet<FileSystemPath> myRootPaths;
        private readonly FileSystemPath mySolutionDirectory;

        public UnityExternalFilesModuleProcessor(Lifetime lifetime, ILogger logger, ISolution solution,
                                                 ChangeManager changeManager,
                                                 IShellLocks locks,
                                                 ISolutionLoadTasksScheduler scheduler,
                                                 IFileSystemTracker fileSystemTracker,
                                                 ProjectFilePropertiesFactory projectFilePropertiesFactory,
                                                 UnityYamlPsiSourceFileFactory psiSourceFileFactory,
                                                 UnityExternalFilesModuleFactory moduleFactory,
                                                 UnityYamlDisableStrategy unityYamlDisableStrategy,
                                                 AssetSerializationMode assetSerializationMode)
        {
            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myChangeManager = changeManager;
            myLocks = locks;
            myFileSystemTracker = fileSystemTracker;
            myProjectFilePropertiesFactory = projectFilePropertiesFactory;
            myPsiSourceFileFactory = psiSourceFileFactory;
            myModuleFactory = moduleFactory;
            myUnityYamlDisableStrategy = unityYamlDisableStrategy;
            myAssetSerializationMode = assetSerializationMode;

            changeManager.RegisterChangeProvider(lifetime, this);

            myRootPaths = new JetHashSet<FileSystemPath>();

            // SolutionDirectory isn't absolute in tests, and will throw an exception if we use it when we call Exists
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory());

            scheduler.EnqueueTask(new SolutionLoadTask(GetType().Name + ".Activate",
                SolutionLoadTaskKinds.PreparePsiModules,
                () => myChangeManager.AddDependency(myLifetime, mySolution.PsiModules(), this)));
        }

        public void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            // For project model access
            myLocks.AssertReadAccessAllowed();

            // Note that this means we process meta and asset files for class library projects, which likely won't have
            // asset files. We can't use IsUnityGeneratedProject because any project based in the Packages folder or
            // using 'file:' won't be processed.
            if (!myAssetSerializationMode.IsForceText || !project.IsUnityProject())
                return;

            var builder = new PsiModuleChangeBuilder();
            var assetFiles = new List<DirectoryEntryData>();

            ProcessUnityProjectDirectories(assetFiles, builder);

            if (assetFiles.Count > 0) myUnityYamlDisableStrategy.Run(assetFiles);

            // See if the project is based on a .asmdef file, and process the files at the .asmdef file location. We'll
            // already have processed any of these files in the Assets and Packages folder, so this will catch any
            // packages that are external to the solution folder and registered with `file:`
            ProcessAsmdefDirectory(assetFiles, builder, project);

            // Add a module reference to the project, so our reference can "see" the target (more accurately, I think
            // this is used to figure out the search domain for Find Usages)
            AddModuleReference(builder, project);

            AddAssetProjectFiles(assetFiles);
            FlushChanges(builder);
        }

        private void ProcessUnityProjectDirectories(List<DirectoryEntryData> assetFiles, PsiModuleChangeBuilder builder)
        {
            // These are idempotent and can be called multiple times. We expect most files to come from here - either in
            // Assets or Packages. We can have assets in externally stored packages, but they are not treated as user
            // editable source, so we can mostly ignore them (the exception is file: based packages, but these are
            // expected to be a small number, and will complicate gathering stats for thresholds)
            ProcessSolutionDirectory(assetFiles, builder, "Assets");
            ProcessSolutionDirectory(assetFiles, builder, "Packages");
            ProcessSolutionDirectory(assetFiles, builder, "ProjectSettings");
        }

        private void ProcessSolutionDirectory(List<DirectoryEntryData> assetFiles, PsiModuleChangeBuilder builder,
                                              string relativePath)
        {
            var path = mySolutionDirectory.Combine(relativePath);
            if (path.ExistsDirectory)
                ProcessDirectory(assetFiles, builder, path);
        }

        private void ProcessAsmdefDirectory(List<DirectoryEntryData> assetFiles, PsiModuleChangeBuilder builder,
                                            IProject project)
        {
            if (!project.IsProjectFromUserView())
                return;

            // We know that the default projects are not .asmdef based, and will obviously live in Assets, which we've
            // already processed. This is just an optimisation - if a plugin renames the projects, we'll still work ok
            if (project.Name == "Assembly-CSharp" || project.Name == "Assembly-CSharp-Editor" ||
                project.Name == "Assembly-CSharp-firstpass" || project.Name == "Assembly-CSharp-Editor-firstpass")
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
                        ProcessDirectory(assetFiles, builder, location.Parent);
                        return;
                    }
                }
            }
        }

        private void ProcessDirectory(List<DirectoryEntryData> assetFiles, PsiModuleChangeBuilder builder,
                                      FileSystemPath directory)
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
            {
                if (entry.RelativePath.IsInterestingMeta())
                {
                    AddMetaPsiSourceFile(builder, entry.GetAbsolutePath());
                }
                else if (entry.RelativePath.IsInterestingAsset())
                {
                    UpdateStatistics(entry);

                    assetFiles.Add(entry);
                }
            }

            myFileSystemTracker.AdviseDirectoryChanges(myLifetime, directory, true, OnProjectDirectoryChange);

            myRootPaths.Add(directory);
        }

        private void AddMetaPsiSourceFile(PsiModuleChangeBuilder builder, FileSystemPath path)
        {
            Assertion.AssertNotNull(myModuleFactory.PsiModule, "myModuleFactory.PsiModule != null");
            if (myModuleFactory.PsiModule.ContainsPath(path))
                return;

            var sourceFile = myPsiSourceFileFactory.CreateExternalPsiSourceFile(myModuleFactory.PsiModule, path);
            builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Added);
        }

        private void UpdateStatistics(DirectoryEntryData directoryEntry)
        {
            if (directoryEntry.RelativePath.IsAsset())
                AssetSizes.Add(directoryEntry.Length);
            else if (directoryEntry.RelativePath.IsPrefab())
                PrefabSizes.Add(directoryEntry.Length);
            else if (directoryEntry.RelativePath.IsScene())
                SceneSizes.Add(directoryEntry.Length);
        }

        private void AddAssetProjectFiles(List<FileSystemPath> paths)
        {
            if (paths.Count == 0)
                return;

            using (new ProjectModelBatchChangeCookie(mySolution, SimpleTaskExecutor.Instance))
            using (mySolution.Locks.UsingWriteLock())
            {
                foreach (var path in paths) AddAssetProjectFile(path);
            }
        }

        private void AddAssetProjectFiles(List<DirectoryEntryData> assetDirectoryEntries)
        {
            if (assetDirectoryEntries.Count == 0)
                return;

            using (new ProjectModelBatchChangeCookie(mySolution, SimpleTaskExecutor.Instance))
            using (mySolution.Locks.UsingWriteLock())
            {
                foreach (var directoryEntry in assetDirectoryEntries)
                    AddAssetProjectFile(directoryEntry.GetAbsolutePath());
            }
        }

        // Add the asset file as a project file, as various features require IProjectFile. Once created, it will
        // automatically get an IPsiSourceFile created for it, and attached to our module via
        // UnityMiscFilesProjectPsiModuleProvider
        private void AddAssetProjectFile(FileSystemPath path)
        {
            if (mySolution.FindProjectItemsByLocation(path).Count > 0)
                return;

            var projectImpl = mySolution.MiscFilesProject as ProjectImpl;
            Assertion.AssertNotNull(projectImpl, "mySolution.MiscFilesProject as ProjectImpl");
            var properties = myProjectFilePropertiesFactory.CreateProjectFileProperties(
                new MiscFilesProjectProperties());
            projectImpl.DoCreateProjectFile(path, properties);
        }

        private void OnProjectDirectoryChange(FileSystemChangeDelta delta)
        {
            var builder = new PsiModuleChangeBuilder();
            var projectFilesToAdd = new List<FileSystemPath>();
            ProcessFileSystemChangeDelta(delta, builder, projectFilesToAdd);
            AddAssetProjectFiles(projectFilesToAdd);
            FlushChanges(builder);
        }

        private void ProcessFileSystemChangeDelta(FileSystemChangeDelta delta, PsiModuleChangeBuilder builder,
            List<FileSystemPath> projectFilesToAdd)
        {
            var module = myModuleFactory.PsiModule;
            if (module == null)
                return;

            IPsiSourceFile sourceFile;
            switch (delta.ChangeType)
            {
                case FileSystemChangeType.ADDED:
                    if (delta.NewPath.IsInterestingAsset())
                        projectFilesToAdd.Add(delta.NewPath);
                    else if (delta.NewPath.IsInterestingMeta())
                        AddMetaPsiSourceFile(builder, delta.NewPath);
                    break;

                case FileSystemChangeType.DELETED:
                    sourceFile = GetYamlPsiSourceFile(module, delta.OldPath);
                    if (sourceFile != null)
                        builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Removed);
                    break;

                case FileSystemChangeType.CHANGED:
                    sourceFile = GetYamlPsiSourceFile(module, delta.NewPath);
                    if (sourceFile != null)
                    {
                        // Make sure we update the cached file system data, or all of our files will have stale
                        // timestamps and never get updated by ICache implementations!
                        if (sourceFile is PsiSourceFileFromPath psiSourceFileFromPath)
                            psiSourceFileFromPath.GetCachedFileSystemData().Refresh(delta.NewPath);
                        builder.AddFileChange(sourceFile, PsiModuleChange.ChangeType.Modified);
                    }
                    break;

                case FileSystemChangeType.SUBTREE_CHANGED:
                case FileSystemChangeType.RENAMED:
                case FileSystemChangeType.UNKNOWN:
                    break;
            }

            foreach (var child in delta.GetChildren())
                ProcessFileSystemChangeDelta(child, builder, projectFilesToAdd);
        }

        [CanBeNull]
        private IPsiSourceFile GetYamlPsiSourceFile(IPsiModuleOnFileSystemPaths module, FileSystemPath path)
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
                        foreach (var fileChange in builder.Result.FileChanges)
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

                        myChangeManager.OnProviderChanged(this, builder.Result, SimpleTaskExecutor.Instance);
                    }
                });
        }

        private void AddModuleReference(PsiModuleChangeBuilder builder, IProject project)
        {
            var thisModule = myModuleFactory.PsiModule;
            if (thisModule == null)
                return;

            foreach (var projectModule in project.GetPsiModules())
                thisModule.AddModuleReference(projectModule);

            builder.AddModuleChange(thisModule, PsiModuleChange.ChangeType.Modified);
        }

        public object Execute(IChangeMap changeMap) => null;
    }
}