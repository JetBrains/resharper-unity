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
            var assetsPathIndex = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse("Assets"));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, assetsPathIndex,
                true);

            var scriptsPathIndex =
                NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse(@"Assets\Scripts"));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, scriptsPathIndex,
                true);
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