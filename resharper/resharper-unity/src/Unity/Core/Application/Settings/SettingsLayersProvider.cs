using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.Application.Settings.Storage.DefaultBody;
using JetBrains.Application.Threading;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    [SolutionComponent]
    public class SettingsLayersProvider : IUnityReferenceChangeHandler
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly IShellLocks myLocks;
        private readonly SettingsStorageProvidersCollection mySettingsStorageProviders;
        private readonly IEnumerable<IUnitySolutionSettingsProvider> mySolutionSettingsProviders;
        private readonly IEnumerable<IUnityProjectSettingsProvider> myProjectSettingsProviders;
        private readonly InternKeyPathComponent myInternedKeyPaths;
        private readonly ILogger myLogger;

        private Dictionary<IProject, SettingsStorageMountPoint> myProjectMountPoints;

        public SettingsLayersProvider(Lifetime lifetime, ISolution solution, IShellLocks locks,
                                      SettingsStorageProvidersCollection settingsStorageProviders,
                                      IEnumerable<IUnitySolutionSettingsProvider> solutionSettingsProviders,
                                      IEnumerable<IUnityProjectSettingsProvider> projectSettingsProviders,
                                      InternKeyPathComponent internedKeyPaths,
                                      ILogger logger)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myLocks = locks;
            mySettingsStorageProviders = settingsStorageProviders;
            mySolutionSettingsProviders = solutionSettingsProviders;
            myProjectSettingsProviders = projectSettingsProviders;
            myInternedKeyPaths = internedKeyPaths;
            myLogger = logger;
        }

        public SettingsStorageMountPoint SolutionMountPoint { get; private set; }

        public void OnHasUnityReference()
        {
            if (SolutionMountPoint == null)
            {
                SolutionMountPoint = CreateSolutionMountPoint();
                foreach (var provider in mySolutionSettingsProviders)
                    provider.InitialiseSolutionSettings(SolutionMountPoint);

                myProjectMountPoints = new Dictionary<IProject, SettingsStorageMountPoint>();
            }
        }

        public void OnUnityProjectAdded(Lifetime projectLifetime, IProject project)
        {
            if (!myProjectMountPoints.ContainsKey(project))
            {
                var mountPoint = CreateProjectMountPoint(projectLifetime, project);
                myProjectMountPoints.Add(project, mountPoint);
                foreach (var provider in myProjectSettingsProviders)
                    provider.InitialiseProjectSettings(projectLifetime, project, mountPoint);
            }
        }

        private SettingsStorageMountPoint CreateProjectMountPoint(Lifetime lifetime, IProject project)
        {
            var name = $"Project {project.Name} (Unity)";
            // Set at a priority less than the .csproj.dotSettings layer, so we can be overridden
            var priority = ProjectModelSettingsStorageMountPointPriorityClasses.ProjectShared * 0.9;
            var isAvailable =
                new IsAvailableByDataConstant<IProject>(lifetime, ProjectModelDataConstants.PROJECT, project,
                    myLocks);
            return CreateMountPoint(lifetime, name, priority, isAvailable, mySettingsStorageProviders);
        }

        private SettingsStorageMountPoint CreateSolutionMountPoint()
        {
            var name = "Solution (Unity)";
            // Set at a priority less than the .sln.dotSettings layer, so we can be overridden
            var priority = ProjectModelSettingsStorageMountPointPriorityClasses.SolutionShared * 0.9;
            var isAvailable = new IsAvailableByDataConstant<ISolution>(myLifetime, ProjectModelDataConstants.SOLUTION,
                mySolution, myLocks);
            return CreateMountPoint(myLifetime, name, priority, isAvailable, mySettingsStorageProviders);
        }

        private SettingsStorageMountPoint CreateMountPoint(Lifetime lifetime, string name, double priority,
            IIsAvailable isAvailable, SettingsStorageProvidersCollection settingsStorageProviders)
        {
            var storage = SettingsStorageFactory.CreateStorage(lifetime, name, myLogger, myInternedKeyPaths);
            var mountPoint = new SettingsStorageMountPoint(storage, SettingsStorageMountPoint.MountPath.Default,
                MountPointFlags.IsDefaultValues, priority, isAvailable, name);

            settingsStorageProviders.MountPoints.Add(lifetime, mountPoint);
            settingsStorageProviders.Storages.Add(lifetime, storage);

            return mountPoint;
        }
    }
}