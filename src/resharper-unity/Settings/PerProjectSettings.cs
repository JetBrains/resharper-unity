using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Storage;
using JetBrains.Application.Settings.Store;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class PerProjectSettings
    {
        private readonly ISettingsSchema settingsSchema;
        private readonly ILogger logger;

        public PerProjectSettings(Lifetime lifetime, IViewableProjectsCollection projects,
                                  ISettingsSchema settingsSchema,
                                  SettingsStorageProvidersCollection settingsStorageProviders, IShellLocks locks,
                                  ILogger logger, InternKeyPathComponent interned)
        {
            this.settingsSchema = settingsSchema;
            this.logger = logger;
            projects.Projects.View(lifetime, (projectLifetime, project) =>
            {
                if (!project.IsUnityProject())
                    return;

                var mountPoint = CreateMountPoint(projectLifetime, project, settingsStorageProviders, locks, logger, interned);
                InitNamespaceProviderSettings(mountPoint);
                InitLanguageLevelSettings(mountPoint);
            });
        }

        private static SettingsStorageMountPoint CreateMountPoint(Lifetime projectLifetime,
                                                                  IProject project, SettingsStorageProvidersCollection settingsStorageProviders,
                                                                  IShellLocks locks, ILogger logger,
                                                                  InternKeyPathComponent interned)
        {
            var storageName = $"Project {project.Name} (Unity)";
            var storage = SettingsStorageFactory.CreateStorage(projectLifetime, storageName, logger, interned);
            var isAvailable = new IsAvailableByDataConstant<IProject>(projectLifetime,
                ProjectModelDataConstants.PROJECT, project, locks);

            // Set at a priority less than the .csproj.dotSettings layer, so we can be overridden
            var priority = ProjectModelSettingsStorageMountPointPriorityClasses.ProjectShared*0.9;
            var mountPoint = new SettingsStorageMountPoint(storage, SettingsStorageMountPoint.MountPath.Default,
                MountPointFlags.IsDefaultValues, priority, isAvailable, storageName);

            settingsStorageProviders.MountPoints.Add(projectLifetime, mountPoint);
            settingsStorageProviders.Storages.Add(projectLifetime, storage);

            return mountPoint;
        }

        private void InitNamespaceProviderSettings(ISettingsStorageMountPoint mountPoint)
        {
            var assetsPathIndex = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse("Assets"));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, assetsPathIndex, true);

            var scriptsPathIndex = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse(@"Assets\Scripts"));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, scriptsPathIndex, true);
        }

        private void InitLanguageLevelSettings(SettingsStorageMountPoint mountPoint)
        {
            // Unity only supports C# 5 for now, but they don't currently put the language level
            // in the csproj (yet - https://twitter.com/jbevain/status/643419833594474496)
            SetValue(mountPoint, (CSharpLanguageProjectSettings s) => s.LanguageLevel, CSharpLanguageLevel.CSharp50);
        }

        private void SetValue<TKeyClass, TEntryValue>([NotNull] ISettingsStorageMountPoint mount,
                                                      [NotNull] Expression<Func<TKeyClass, TEntryValue>> lambdaexpression, [NotNull] TEntryValue value,
                                                      IDictionary<SettingsKey, object> keyIndices = null)
        {
            ScalarSettingsStoreAccess.SetValue(mount, settingsSchema.GetScalarEntry(lambdaexpression), keyIndices, value,
                false, null, logger);
        }

        private void SetIndexedValue<TKeyClass, TEntryIndex, TEntryValue>([NotNull] ISettingsStorageMountPoint mount,
                                                                          [NotNull] Expression<Func<TKeyClass, IIndexedEntry<TEntryIndex, TEntryValue>>> lambdaexpression,
                                                                          [NotNull] TEntryIndex index,
                                                                          [NotNull] TEntryValue value,
                                                                          IDictionary<SettingsKey, object> keyIndices = null)
        {
            ScalarSettingsStoreAccess.SetIndexedValue(mount, settingsSchema.GetIndexedEntry(lambdaexpression), index,
                keyIndices, value, null, logger);
        }
    }
}