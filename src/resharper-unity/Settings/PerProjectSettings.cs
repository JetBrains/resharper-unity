using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Storage;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Properties.Flavours;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class PerProjectSettings
    {
        public PerProjectSettings(Lifetime lifetime, IViewableProjectsCollection projects,
                                  SettingsStorageProvidersCollection settingsStorageProviders, IShellLocks locks,
                                  ILogger logger, InternKeyPathComponent interned)
        {
            projects.Projects.View(lifetime, (projectLifetime, project) =>
            {
                if (!project.HasFlavour<UnityProjectFlavor>())
                    return;

                CreateMountPoint(projectLifetime, project, settingsStorageProviders, locks, logger, interned);
            });
        }

        private static void CreateMountPoint(Lifetime projectLifetime,
                                             IProject project, SettingsStorageProvidersCollection settingsStorageProviders,
                                             IShellLocks locks, ILogger logger,
                                             InternKeyPathComponent interned)
        {
            var storageName = $"Project {project.Name} (Unity)";
            var storage = SettingsStorageFactory.CreateStorage(projectLifetime, storageName, logger, interned);
            var isAvailable = new IsAvailableByDataConstant<IProject>(projectLifetime,
                ProjectModelDataConstants.Project, project, locks);

            // Set at a priority less than the .csproj.dotSettings layer, so we can be overridden
            var priority = ProjectModelSettingsStorageMountPointPriorityClasses.ProjectShared*0.9;
            var mountPoint = new SettingsStorageMountPoint(storage, SettingsStorageMountPoint.MountPath.Default,
                MountPointFlags.IsDefaultValues, priority, isAvailable, storageName);

            settingsStorageProviders.MountPoints.Add(projectLifetime, mountPoint);
            settingsStorageProviders.Storages.Add(projectLifetime, storage);
        }
    }
}