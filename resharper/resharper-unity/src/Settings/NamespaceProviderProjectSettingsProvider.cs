using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class NamespaceProviderProjectSettingsProvider : IUnityProjectSettingsProvider
    {
        private readonly ISettingsSchema mySettingsSchema;
        private readonly ILogger myLogger;

        public NamespaceProviderProjectSettingsProvider(ISettingsSchema settingsSchema, ILogger logger)
        {
            mySettingsSchema = settingsSchema;
            myLogger = logger;
        }

        public void InitialiseProjectSettings(Lifetime projectLifetime, IProject project,
                                              ISettingsStorageMountPoint mountPoint)
        {
            // Don't require Assets or Assets.Scripts in namespaces
            ExcludeFolderFromNamespace(mountPoint, "Assets");
            ExcludeFolderFromNamespace(mountPoint, @"Assets\Scripts");

            ExcludePackagesFoldersFromNamespace(mountPoint, project, "Packages");
            foreach (var projectFolder in project.GetSubFolders("Library"))
            {
                ExcludeFolderFromNamespace(mountPoint, "Library");
                ExcludePackagesFoldersFromNamespace(mountPoint, projectFolder, "PackageCache", @"Library");
            }
        }

        private void ExcludeFolderFromNamespace(ISettingsStorageMountPoint mountPoint, string path)
        {
            var index = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse(path,
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
            // First time called, this excludes package name, e.g. `Packages/com.unity.mathematics` or
            // `Library/PackageCache/com.unity.collections@0.0.9-preview.9`
            // If that folder does not have a package.json recurse until we find one, and exclude everything along the
            // way. This handles an edge case where e.g. a GitHub repo is checked out into Packages, but the root of the
            // project isn't the package. Instead, the package is defined one or two levels down, and included into the
            // project via file: in the manifest.
            // E.g. myrepo/src/mypackage/{package.json}. We don't want `myrepo` or `src` appearing in the namespace
            foreach (var subFolder in thisFolder.GetSubFolders())
            {
                var path = thisPath + @"\" + subFolder.Name;
                ExcludeFolderFromNamespace(mountPoint, path);

                // Is it a package folder? Exclude Scripts, Runtime, Scripts/Runtime and Runtime/Scripts
                if (subFolder.Location.Combine("package.json").ExistsFile)
                {
                    ExcludeFolderFromNamespace(mountPoint, path + @"\Runtime");
                    ExcludeFolderFromNamespace(mountPoint, path + @"\Scripts");
                    ExcludeFolderFromNamespace(mountPoint, path + @"\Runtime\Scripts");
                    ExcludeFolderFromNamespace(mountPoint, path + @"\Scripts\Runtime");
                    return;
                }

                ExcludePackageSubFoldersFromNamespace(mountPoint, subFolder, path);
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