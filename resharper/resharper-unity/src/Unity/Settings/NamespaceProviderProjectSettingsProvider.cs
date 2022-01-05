using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class NamespaceProviderProjectSettingsProvider : IUnityProjectSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public NamespaceProviderProjectSettingsProvider(ISettingsSchema settingsSchema, ILogger logger, UnitySolutionTracker unitySolutionTracker)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
            myUnitySolutionTracker = unitySolutionTracker;
        }

        // The reasoning behind this is fairly simple:
        // * Package location should not affect namespace
        //   The package owner is not in control of the location of the package on an end user's system. It might be
        //   cloned into the Packages folder, or referenced and cached in the Library/PackageCache folder. It might also
        //   be referenced in an external location with the file: syntax
        // * Assembly definition location should affect location, but only relative to Assets or package root folder
        //   Packages are self contained units. The location of an assembly definition in a folder structure (especially
        //   the Assets folder) is deliberate on the part of the user and should be part of the namespace
        // * If the asmdef defines a root namespace (Unity 2020.2a12+) this overrides everything, up to and including
        //   the location of the .asmdef file
        // * Exclude some folder names, based on observed convention. E.g. Assets, Runtime, Scripts.
        //   Editor is deliberately not excluded.
        //
        // There are subtleties:
        // * Exclude Assets and Assets/Scripts, Packages and Library/PackageCache
        // * Exclude the package root folder. The package owner is not in control of this.
        //   Referenced packages are stored in Library/PackageCache with a folder name based on ID and version
        //   End users can clone git repos or tarballs into any folder
        // * Exclude any folder until we get to the package root (the location of the package.json)
        //   If we clone a git repo into Packages, and the repo doesn't have a package in the root of the repo, skip all
        //   folders until we get to the package root. The package can be included via a file: entry in manifest.json
        // * A package that lives inside the solution structure, but outside of Assets or Packages will have a project
        //   with a path that is relative to the solution root. Ignore all intermediary folders between solution and
        //   package root
        // * A package that lives outside of the solution structure will have a root folder that is a link to the common
        //   parent of all files in the project. This is usually the asmdef location, AND IS INCORRECT.
        //   The linked folder is not included in namespace suggestions
        //   TODO: The linked folder should be the package root folder (which we normally ignore)
        //   This requires changes to the generated files. Either we include package.json to make a new default folder,
        //   or we add Link attributes to each file item
        // * After package root, ignore Runtime, Scripts and any combination of the two
        //
        // And implications:
        // * If there is no root namespace specified for an assembly definition in a package, one can be inferred from
        //   the path from the package root to the assembly definition file
        //   E.g. /Packages/com.unity.collections@0.0.9-preview.9/Unity.Collections/Unity.Collections.asmdef will have
        //   a "root namespace" of Unity.Collections
        public void InitialiseProjectSettings(Lifetime projectLifetime, IProject project,
                                              ISettingsStorageMountPoint mountPoint)
        {
            if (!myUnitySolutionTracker.IsUnityProject.HasTrueValue())
                return;
            
            ExcludeFolderFromNamespace(mountPoint, "Assets");
            ExcludeFolderFromNamespace(mountPoint, @"Assets\Scripts");

            ExcludePackagesFoldersFromNamespace(mountPoint, project, "Packages");
            foreach (var projectFolder in project.GetSubFolders("Library"))
            {
                ExcludeFolderFromNamespace(mountPoint, "Library");
                ExcludePackagesFoldersFromNamespace(mountPoint, projectFolder, "PackageCache", @"Library");
            }

            ExcludeExternalPackagesFromNamespace(mountPoint, project);
        }

        private void ExcludeFolderFromNamespace(ISettingsStorageMountPoint mountPoint, string path)
        {
            var index = NamespaceFolderProvider.GetIndexFromOldIndex(VirtualFileSystemPath.Parse(path, InteractionContext.SolutionContext,
                FileSystemPathInternStrategy.TRY_GET_INTERNED_BUT_DO_NOT_INTERN));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, index, true);
        }

        private void ExcludePackagesFoldersFromNamespace(ISettingsStorageMountPoint mountPoint,
                                                         IProjectFolder packageHierarchyParentFolder,
                                                         string packageHierarchyFolderName,
                                                         string parentProjectRelativePath = "")
        {
            var packageHierarchyRootFolderPath = parentProjectRelativePath.Length == 0
                ? packageHierarchyFolderName
                : parentProjectRelativePath + @"\" + packageHierarchyFolderName;

            foreach (var subFolder in packageHierarchyParentFolder.GetSubFolders(packageHierarchyFolderName))
            {
                // Exclude the root of the package hierarchy (Packages or PackageCache)
                ExcludeFolderFromNamespace(mountPoint, packageHierarchyRootFolderPath);
                ExcludePackageSubFoldersFromNamespace(mountPoint, subFolder, packageHierarchyRootFolderPath);
            }
        }

        private void ExcludePackageSubFoldersFromNamespace(ISettingsStorageMountPoint mountPoint,
                                                           IProjectFolder thisFolder, string thisPath)
        {
            foreach (var subFolder in thisFolder.GetSubFolders())
            {
                var path = thisPath + @"\" + subFolder.Name;
                ExcludePackageRootFolderFromNamespace(mountPoint, subFolder, path);
            }
        }

        private void ExcludePackageRootFolderFromNamespace(ISettingsStorageMountPoint mountPoint,
            IProjectFolder folder, string path)
        {
            ExcludeFolderFromNamespace(mountPoint, path);

            // Is it a package folder? Exclude Scripts, Runtime, Scripts/Runtime and Runtime/Scripts
            if (folder.Location.Combine("package.json").ExistsFile)
            {
                // The folder will be linked if the project's files belong to a package that is external to the solution
                // folder. With a linked folder, the namespace provider must be the actual path, relative to the parent
                // folder, rather than the visible path in the Solution Explorer
                //
                // NOTE: KEEP UP TO DATE WITH ExternalPackageCustomNamespaceProvider!
                if (folder.IsLinked && folder.ParentFolder != null)
                    path = folder.Location.ConvertToRelativePath(folder.ParentFolder.Location).FullPath;
                ExcludeFolderFromNamespace(mountPoint, path + @"\Runtime");
                ExcludeFolderFromNamespace(mountPoint, path + @"\Scripts");
                ExcludeFolderFromNamespace(mountPoint, path + @"\Runtime\Scripts");
                ExcludeFolderFromNamespace(mountPoint, path + @"\Scripts\Runtime");
                return;
            }

            // Recurse until we hit the package root
            ExcludePackageSubFoldersFromNamespace(mountPoint, folder, path);
        }

        private void ExcludeExternalPackagesFromNamespace(ISettingsStorageMountPoint mountPoint, IProject project)
        {
            // Handle assembly definitions for packages that live outside of the Unity project structure (i.e. not under
            // Assets, Packages or Library/PackageCache), or that lives outside of the solution completely.
            // If the assembly definition lives outside of the solution, this folder will be a link to the assembly
            // definition location, NOT the package root. If it lives in the Unity solution folder, it will be the start
            // of the path to the project root
            foreach (var projectFolder in project.GetSubFolders())
            {
                if (projectFolder.Name.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                    || projectFolder.Name.Equals("Library", StringComparison.OrdinalIgnoreCase)
                    || projectFolder.Name.Equals("Packages", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ExcludePackageRootFolderFromNamespace(mountPoint, projectFolder, projectFolder.Name);
            }
        }

        private void SetIndexedValue<TKeyClass, TEntryIndex, TEntryValue>([NotNull] ISettingsStorageMountPoint mount,
                                                                          [NotNull] Expression<Func<TKeyClass, IIndexedEntry<TEntryIndex, TEntryValue>>> lambdaexpression,
                                                                          [NotNull] TEntryIndex index,
                                                                          [NotNull] TEntryValue value,
                                                                          IDictionary<SettingsKey, object> keyIndices = null)
        {
            ScalarSettingsStoreAccess.SetIndexedValue(mount, mySettingsSchema.GetIndexedEntry(lambdaexpression), index,
                keyIndices, value, null, myLogger);
        }
    }
}