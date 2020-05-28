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

            // Don't require Packages in namespaces
            ExcludeFolderFromNamespace(mountPoint, "Packages");

            // The subfolder of Packages is the root of the package, don't require this in the namespace
            // E.g. Given Packages/com.foo.package/package.json don't require Packages.com.foo.packages in the namespace
            foreach (var projectFolder in project.GetSubFolders("Packages"))
            {
                foreach (var subFolder in projectFolder.GetSubFolders())
                {
                    ExcludeFolderFromNamespace(mountPoint, projectFolder.Name + @"\" + subFolder.Name);
                }
            }
        }

        private void ExcludeFolderFromNamespace(ISettingsStorageMountPoint mountPoint, string path)
        {
            var index = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse(path, FileSystemPathInternStrategy.TRY_GET_INTERNED_BUT_DO_NOT_INTERN));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, index, true);
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