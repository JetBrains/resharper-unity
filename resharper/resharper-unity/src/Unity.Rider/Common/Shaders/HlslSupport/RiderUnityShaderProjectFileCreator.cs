#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.Application.Progress;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Transaction;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Core;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders.HlslSupport
{
    // Adds a package shader file that no generated .csproj lists to its package's project, so the C++ engine
    // analyses it. A package without a generated project is left alone.
    // Lives in Common because the Integration zone is off in tests.
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class RiderUnityShaderProjectFileCreator : IUnityExternalProjectFileCreator
    {
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly IShellLocks myLocks;
        private readonly PackageManager myPackageManager;

        public RiderUnityShaderProjectFileCreator(Lifetime lifetime,
                                                  ILogger logger,
                                                  ISolution solution,
                                                  IShellLocks locks,
                                                  PackageManager packageManager)
        {
            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myLocks = locks;
            myPackageManager = packageManager;
        }

        // Marks the items this creator made, apart from the project's own items at the same location.
        private sealed class ShaderProjectElementOrigin : IProjectElementOrigin
        {
            public static readonly IProjectElementOrigin Instance = new ShaderProjectElementOrigin();
            private ShaderProjectElementOrigin() { }
            public bool CanModify(out string? reason) { reason = null; return true; }
            public override string ToString() => "UnityShaderProjectFile";
        }

        public bool OwnsItem(IProjectItem item) =>
            item is IProjectFile file && ReferenceEquals(file.Origin, ShaderProjectElementOrigin.Instance);

        public bool RequiresProjectFile(IPath path) => UnityShaderFileUtils.IsShaderPsiExtension(path.ExtensionWithDot);

        public void CreateProjectFiles(IReadOnlyList<VirtualFileSystemPath> paths,
                                       Action<IReadOnlyList<VirtualFileSystemPath>> onAdopted)
        {
            myLocks.ExecuteOrQueueEx(myLifetime, "UnityShaderProjectFiles",
                () => CreateProjectFilesCore(paths, onAdopted));
        }

        private void CreateProjectFilesCore(IReadOnlyList<VirtualFileSystemPath> paths,
                                            Action<IReadOnlyList<VirtualFileSystemPath>> onAdopted)
        {
            // Filesystem work first, outside the write lock, so the disk does not block the UI.
            var candidatesByDirectory =
                new Dictionary<VirtualFileSystemPath, IReadOnlyList<IReadOnlyList<VirtualFileSystemPath>>>();
            foreach (var path in paths)
            {
                var directory = path.Parent;
                if (!candidatesByDirectory.ContainsKey(directory))
                    candidatesByDirectory.Add(directory, FindGoverningAsmdefs(directory));
            }

            var propertiesFactory = mySolution.GetComponent<ProjectFilePropertiesFactory>();

            var adopted = new List<VirtualFileSystemPath>();

            // Report the adopted paths even when the loop throws, or their items are never tracked.
            try
            {
                using (new ProjectModelBatchChangeCookie(mySolution, SimpleTaskExecutor.Instance))
                using (myLocks.UsingWriteLock())
                {
                    var ownerByDirectory = new Dictionary<VirtualFileSystemPath, IProject?>();
                    var createdByProject = new Dictionary<IProject, int>();
                    var skipped = 0;

                    foreach (var path in paths)
                    {
                        var directory = path.Parent;
                        if (!ownerByDirectory.TryGetValue(directory, out var owner))
                        {
                            owner = ResolveOwner(candidatesByDirectory[directory]);
                            ownerByDirectory.Add(directory, owner);
                        }

                        if (owner is not ProjectFolderImpl projectFolder)
                        {
                            skipped++;
                            continue;
                        }

                        // Adopted but not created: a full sync can drop the existing item later.
                        if (mySolution.HasRealProjectItem(path))
                        {
                            adopted.Add(path);
                            continue;
                        }

                        var properties = propertiesFactory.CreateProjectFileProperties(owner.ProjectProperties);
                        projectFolder.DoCreateFile(path.Name, path, properties, ShaderProjectElementOrigin.Instance);
                        createdByProject.TryGetValue(owner, out var count);
                        createdByProject[owner] = count + 1;
                        adopted.Add(path);
                    }

                    if (skipped > 0)
                        myLogger.Verbose("Left {0} shader files alone: no project covers their package", skipped);

                    foreach (var pair in createdByProject)
                        myLogger.Verbose("Created {0} shader project files in {1}", pair.Value, pair.Key.Name);
                }
            }
            finally
            {
                onAdopted(adopted);
            }
        }

        // Returns the .asmdef files of the path's package, grouped by directory, nearest ancestor first.
        // The last group holds every .asmdef in the package, editor-only ones last. A path outside a package gets none.
        private IReadOnlyList<IReadOnlyList<VirtualFileSystemPath>> FindGoverningAsmdefs(VirtualFileSystemPath directory)
        {
            var packageFolder = myPackageManager.GetOwningPackage(directory)?.PackageFolder;
            if (packageFolder == null || packageFolder.IsEmpty)
                return EmptyList<IReadOnlyList<VirtualFileSystemPath>>.Instance;

            var groups = new List<IReadOnlyList<VirtualFileSystemPath>>();
            for (var current = directory; current != null && !current.IsEmpty; current = current.Parent)
            {
                if (current.ExistsDirectory)
                {
                    var here = current.GetChildFiles("*" + UnityFileExtensions.AsmDefFileExtensionWithDot);
                    if (here.Count > 0)
                        groups.Add(here.OrderBy(p => p.Name, StringComparer.Ordinal).ToList());
                }

                if (current == packageFolder)
                    break;
            }

            groups.Add(packageFolder.GetChildFiles("*" + UnityFileExtensions.AsmDefFileExtensionWithDot, PathSearchFlags.RecurseIntoSubdirectories)
                .Where(p => !p.IsUnderHiddenAssetFolder(packageFolder))
                .OrderBy(p => p.IsUnderEditorFolder(packageFolder))
                .ThenBy(p => p.FullPath.Length)
                .ThenBy(p => p.Name, StringComparer.Ordinal)
                // Total order, so a rescan does not move an item to another project.
                .ThenBy(p => p.FullPath, StringComparer.Ordinal)
                .ToList());

            return groups;
        }

        // Call under the lock. Filters before picking, because the .Player twin claims the same .asmdef.
        private IProject? ResolveOwner(IReadOnlyList<IReadOnlyList<VirtualFileSystemPath>> groups)
        {
            foreach (var group in groups)
            {
                foreach (var asmdef in group)
                {
                    var owner = mySolution.FindRealProjectItemsByLocation(asmdef)
                        .Select(item => item.GetProject())
                        .Where(IsSuitable)
                        .OrderBy(project => project!.Name, StringComparer.Ordinal)
                        .FirstOrDefault();

                    if (owner != null)
                        return owner;
                }
            }

            return null;
        }

        // Without a UnityShaderModule the item gets no PSI.
        private static bool IsSuitable(IProject? project) =>
            project != null && project.IsValid() && UnityShaderFileUtils.IsShaderModuleProject(project);
    }
}
