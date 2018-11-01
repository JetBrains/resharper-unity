using System;
using System.Collections.Generic;
using JetBrains.Application.changes;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Tasks;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Modules.ExternalFileModules;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.dataStructures;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Modules
{
    // * AdditionalFilesModuleFactoryBase isn't a base class for implementing IPsiModuleFactory, but is intended to help
    //   build/populate other PSI modules (that contain additional files).
    // * This class populates and maintains the module created by UnityExternalFilesModuleFactory. It's currently
    //   interested in .unity files (so we can add references from YAML to methods) and .cs.meta files (so we can
    //   generate a cache of GUIDs for MonoScript assets so we know what classes the YAML references are pointing to).
    //   Also .asset files, and ProjectSettings/*
    // * We need to collect external files from the main project, editor project, first pass projects, asmdef files and
    //   source packages
    // * This means we need to add everything under Assets and Packages, and also the root folder of any package that
    //   isn't under Assets or Packages. This will handle file based packages
    // TODO: Do we need to do anything for read only, "referenced" packages?
    // The contents of a read only, "referenced" package are treated as assets, so AIUI, a package could contain a scene
    // file that had references to methods in that package. These packages are not editable, and are not source packages
    // - they are compiled and added as assemblies, so we will never see either the scene file, the source or the meta
    // files, so I _think_ we can happily ignore these packages.
    // If we ever create PSI module(s) for referenced packages (to enable better browsing), then this will change
    [SolutionComponent]
    public class UnityExternalFilesModuleProcessor : AdditionalFilesModuleFactoryBase, IUnityReferenceChangeHandler
    {
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly IPsiSourceFileProperties myPsiSourceFileProperties;
        private readonly JetHashSet<FileSystemPath> myRootPaths;
        private readonly GroupingEvent myGroupingEvent;
        private readonly FileSystemPath mySolutionDirectory;
        private JetHashSet<FileSystemPath> myFileChanges = new JetHashSet<FileSystemPath>();

        public UnityExternalFilesModuleProcessor(Lifetime lifetime, ISolution solution, ChangeManager changeManager,
                                                 PsiProjectFileTypeCoordinator coordinator,
                                                 IProjectFileExtensions extensions,
                                                 IShellLocks locks,
                                                 ISolutionLoadTasksScheduler scheduler,
                                                 IFileSystemTracker fileSystemTracker,
                                                 UnityExternalFilesModuleFactory psiModuleFactory)
            : base(solution, changeManager, coordinator, extensions, lifetime, locks, psiModuleFactory.PsiModule)
        {
            myFileSystemTracker = fileSystemTracker;
            myPsiSourceFileProperties = new UnityExternalFileProperties();
            myRootPaths = new JetHashSet<FileSystemPath>();

            // SolutionDirectory isn't absolute in tests, so resolve it. If it's not absolute and we call Exists, we'll
            // get an exception (and break ALL the tests)
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory());

            myGroupingEvent = locks.GroupingEvents.CreateEvent(lifetime, GetType().Name + ".FileChanges",
                TimeSpan.FromMilliseconds(50.0), Rgc.Guarded, FlushFileChanges);

            // Make sure our module change notifications propagate correctly
            scheduler.EnqueueTask(new SolutionLoadTask(GetType().Name + ".Activate",
                SolutionLoadTaskKinds.PreparePsiModules,
                () => myChangeManager.AddDependency(myLifetime, Solution.PsiModules(), this)));
        }

        // Keep the file in the module after it's closed in the (VS) misc project?
        protected override bool MustAlwaysHaveAdditionalFile(FileSystemPath oldLocation) => true;

        protected override bool ShouldAcceptMiscProjectFile(IProjectFile projectFile)
        {
            return IsInterestingFile(projectFile.Location);
        }

        protected override IPsiSourceFileProperties CreateProperties() => myPsiSourceFileProperties;

        // The module monitors added files for changes. We just need to notify that our file has changed
        protected override void OnFileChange(FileSystemChangeDelta delta)
        {
            if (delta.ChangeType == FileSystemChangeType.CHANGED && IsInterestingFile(delta.NewPath))
            {
                lock (this)
                    myFileChanges.Add(delta.NewPath);
                myGroupingEvent.FireIncoming();
            }
        }

        private void FlushFileChanges()
        {
            JetHashSet<FileSystemPath> fileChanges;
            lock (this)
            {
                if (myFileChanges.Count == 0)
                    return;
                fileChanges = myFileChanges;
                myFileChanges = new JetHashSet<FileSystemPath>();
            }

            var builder = new PsiModuleChangeBuilder();
            foreach (var fileSystemPath in fileChanges)
            {
                if (PsiModule.TryGetFileByPath(fileSystemPath, out var file))
                    builder.AddFileChange(file, PsiModuleChange.ChangeType.Modified);
            }

            if (!builder.IsEmpty)
            {
                myLocks.ExecuteOrQueueEx(myLifetime, GetType().Name + ".FlushFileChanges",
                    () => PropagateChanges(builder, true));
            }
        }

        public void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            // TODO: Only process the project if YAML parsing is enabled

            var added = new List<FileSystemPath>();
            ProcessAssets(added);
            ProcessPackages(added);
            ProcessProjectSettings(added);

            // Look at each project being added. If the project folder is not under Assets or Packages, handle it
            if (project.IsProjectFromUserView())
            {
                // This assumes that all of the files in a project are in the same folder
                // TODO: Is this a reasonable assumption?
                var projectDirectory = project.Location;
                if (!mySolutionDirectory.IsPrefixOf(projectDirectory))
                    ProcessFolder(projectDirectory, added);
            }

            if (added.Count > 0)
                FlushChanges(added, EmptyList<FileSystemPath>.Instance, true);

            AddModuleReference(project);
        }

        private void ProcessAssets(List<FileSystemPath> added)
        {
            var assetsPath = mySolutionDirectory.Combine("Assets");
            if (assetsPath.ExistsDirectory)
                ProcessFolder(assetsPath, added);
        }

        private void ProcessPackages(List<FileSystemPath> added)
        {
            var packagesPath = mySolutionDirectory.Combine("Packages");
            if (packagesPath.ExistsDirectory)
                ProcessFolder(packagesPath, added);
        }

        private void ProcessProjectSettings(List<FileSystemPath> added)
        {
            // This doesn't handle ProjectSettings/ProjectVersion.txt, but I don't think we really care about that
            var projectSettingsPath = mySolutionDirectory.Combine("ProjectSettings");
            if (projectSettingsPath.ExistsDirectory)
                ProcessFolder(projectSettingsPath, added);
        }

        private void ProcessFolder(FileSystemPath folder, List<FileSystemPath> added)
        {
            if (myRootPaths.Contains(folder))
                return;

            // TODO: Only process .unity if the project is set to text serialisation
            AddFiles(folder, added, "*.cs.meta");
            AddFiles(folder, added, "*.unity");
            AddFiles(folder, added, "*.asset");

            myFileSystemTracker.AdviseDirectoryChanges(myLifetime, folder, true, OnProjectDirectoryChanged);

            myRootPaths.Add(folder);
        }

        private void AddFiles(FileSystemPath folder, List<FileSystemPath> added, string filePattern)
        {
            // TODO: Is this pattern case insensitive?
            var metaFiles = folder.GetChildFiles(filePattern, PathSearchFlags.RecurseIntoSubdirectories);
            added.AddRange(metaFiles);
        }

        private void OnProjectDirectoryChanged(FileSystemChangeDelta delta)
        {
            var added = new FrugalLocalList<FileSystemPath>();
            var removed = new FrugalLocalList<FileSystemPath>();

            ProcessFileSystemDelta(delta, ref added, ref removed);

            if (added.Count > 0 || removed.Count > 0)
                FlushChanges(added.ToArray(), removed.ToArray(), true);
        }

        private void ProcessFileSystemDelta(FileSystemChangeDelta delta, ref FrugalLocalList<FileSystemPath> added,
                                            ref FrugalLocalList<FileSystemPath> removed)
        {
            switch (delta.ChangeType)
            {
                case FileSystemChangeType.ADDED:
                    if (IsInterestingFile(delta.NewPath))
                        added.Add(delta.NewPath);
                    break;
                case FileSystemChangeType.DELETED:
                    if (IsInterestingFile(delta.OldPath))
                        removed.Add(delta.OldPath);
                    break;
                case FileSystemChangeType.CHANGED:
                case FileSystemChangeType.SUBTREE_CHANGED:
                case FileSystemChangeType.RENAMED:
                case FileSystemChangeType.UNKNOWN:
                    break;
            }

            foreach (var child in delta.GetChildren())
                ProcessFileSystemDelta(child, ref added, ref removed);
        }

        // We need to add a module reference to the C# project so that searching for method usages will include this
        // module in the search scope. This makes sense - this module is conceptually the same as the Unity project
        // (i.e. Assets + Packages) and it needs to take a dependency on the C# projects in order to correctly see the
        // methods
        private void AddModuleReference(IProject project)
        {
            var thisModule = (UnityExternalFilesPsiModule) PsiModule;
            foreach (var module in project.GetPsiModules())
                thisModule.AddModuleReference(module);

            var builder = new PsiModuleChangeBuilder();
            builder.AddModuleChange(thisModule, PsiModuleChange.ChangeType.Modified);

            myLocks.ExecuteOrQueueEx(myLifetime, GetType().Name + ".FlushModuleChanges",
                () => PropagateChanges(builder, true));
        }

        private static bool IsInterestingFile(IPath path)
        {
            // TODO: Only process .unity if the project is set to text serialisation
            // TODO: Should we check for .cs.meta instead of just .meta?
            var extension = path.ExtensionNoDot;
            return string.Equals(extension, "unity", StringComparison.InvariantCultureIgnoreCase)
                   || string.Equals(extension, "meta", StringComparison.InvariantCultureIgnoreCase)
                   || string.Equals(extension, "asset", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}