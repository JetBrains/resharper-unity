using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.changes;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Storage;
using JetBrains.Application.Settings.Store;
using JetBrains.Application.Settings.Store.Implementation;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Settings.Storages;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class PerProjectSettings : IChangeProvider
    {
        private static readonly Version Version46 = new Version(4, 6);

        private readonly ISolution mySolution;
        private readonly ChangeManager myChangeManager;
        private readonly ISettingsSchema settingsSchema;
        private readonly SettingsStorageProvidersCollection mySettingsStorageProviders;
        private readonly IShellLocks myLocks;
        private readonly ILogger logger;
        private readonly InternKeyPathComponent myInterned;
        private readonly LangVersionCacheProvider myLangVersionCache;
        private readonly Dictionary<IProject, SettingsStorageMountPoint> myProjectMountPoints;
        private readonly Dictionary<IProject, Lifetime> myProjectLifetimes;

        public PerProjectSettings(Lifetime lifetime, ISolution solution, ChangeManager changeManager,
                                  ModuleReferenceResolveSync moduleReferenceResolveSync,
                                  IViewableProjectsCollection projects,
                                  ISettingsSchema settingsSchema,
                                  SettingsStorageProvidersCollection settingsStorageProviders, IShellLocks locks,
                                  ILogger logger, InternKeyPathComponent interned,
                                  LangVersionCacheProvider langVersionCache)
        {
            mySolution = solution;
            myChangeManager = changeManager;
            this.settingsSchema = settingsSchema;
            mySettingsStorageProviders = settingsStorageProviders;
            myLocks = locks;
            this.logger = logger;
            myInterned = interned;
            myLangVersionCache = langVersionCache;
            myProjectMountPoints = new Dictionary<IProject, SettingsStorageMountPoint>();
            myProjectLifetimes = new Dictionary<IProject, Lifetime>();

            changeManager.RegisterChangeProvider(lifetime, this);
            changeManager.AddDependency(lifetime, this, moduleReferenceResolveSync);

            projects.Projects.View(lifetime, (projectLifetime, project) =>
            {
                myProjectLifetimes.Add(project, projectLifetime);

                if (!project.IsUnityProject())
                    return;

                InitialiseProjectSettings(project);
            });
        }

        object IChangeProvider.Execute(IChangeMap changeMap)
        {
            var projectModelChange = changeMap.GetChange<ProjectModelChange>(mySolution);
            if (projectModelChange == null)
                return null;

            // ReSharper hasn't necessarily processed all references when it adds the IProject
            // to the IViewableProjectsCollection. Keep an eye on reference changes, add the
            // project settings if/when the project becomes a unity project
            var projects = new JetHashSet<IProject>();
            var changes = ReferencedAssembliesService.TryGetAssemblyReferenceChanges(projectModelChange, ProjectExtensions.UnityReferenceNames);
            foreach (var change in changes)
                projects.Add(change.GetNewProject());

            foreach (var project in projects)
            {
                myChangeManager.ExecuteAfterChange(() =>
                {
                    if (project.IsUnityProject())
                        InitialiseProjectSettings(project);
                });
            }

            return null;
        }

        private void InitialiseProjectSettings(IProject project)
        {
            Lifetime projectLifetime;
            if (!myProjectLifetimes.TryGetValue(project, out projectLifetime))
                return;

            SettingsStorageMountPoint mountPoint;
            lock(myProjectMountPoints)
            {
                // There's already a mount point, we're already initialised
                if (myProjectMountPoints.TryGetValue(project, out mountPoint))
                    return;

                mountPoint = CreateMountPoint(projectLifetime, project, mySettingsStorageProviders, myLocks, logger,
                    myInterned);
                myProjectMountPoints.Add(projectLifetime, project, mountPoint);
            }

            InitialiseSettingValues(project, mountPoint);

            // Just to make things more interesting, the langversion cache isn't
            // necessarily updated by the time we get called, so wire up a callback
            myLangVersionCache.RegisterDataChangedCallback(projectLifetime, project.ProjectFileLocation,
                () => InitialiseSettingValues(project, mountPoint));
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

        private void InitialiseSettingValues(IProject project, SettingsStorageMountPoint mountPoint)
        {
            InitNamespaceProviderSettings(mountPoint);
            InitLanguageLevelSettings(project, mountPoint);
        }

        private void InitNamespaceProviderSettings(ISettingsStorageMountPoint mountPoint)
        {
            var assetsPathIndex = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse("Assets"));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, assetsPathIndex, true);

            var scriptsPathIndex = NamespaceFolderProvider.GetIndexFromOldIndex(FileSystemPath.Parse(@"Assets\Scripts"));
            SetIndexedValue(mountPoint, NamespaceProviderSettingsAccessor.NamespaceFoldersToSkip, scriptsPathIndex, true);
        }

        private void InitLanguageLevelSettings(IProject project, SettingsStorageMountPoint mountPoint)
        {
            // Make sure ReSharper doesn't suggest code changes that won't compile in Unity
            // due to mismatched C# language levels (e.g. C#6 "elvis" operator)
            //
            // * Unity prior to 5.5 uses an old mono compiler that only supports C# 4
            // * Unity 5.5 and later adds C# 6 support as an option. This is enabled by setting
            //   the API compatibility level to NET_4_6
            // * The CSharp60Support plugin replaces the compiler with either C# 6 or C# 7.0
            //   It can be recognised by a folder called `CSharp60Support` or `CSharp70Support`
            //   in the root of the project
            //   (https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration)
            //
            // Scenarios:
            // * No VSTU installed (including Unity 5.5)
            //   .csproj has NO `LangVersion`. `TargetFrameworkVersion` will be `v3.5`
            // * Early versions of VSTU
            //   .csproj has NO `LangVersion`. `TargetFrameworkVersion` will be `v3.5`
            // * Later versions of VSTU
            //   `LangVersion` is correctly set to "4". `TargetFrameworkVersion` will be `v3.5`
            // * VSTU for 5.5
            //   `LangVersion` is set to "default". `TargetFrameworkVersion` will be `v3.5` or `v4.6`
            //   Note that "default" for VS"15" or Rider will be C# 7.0!
            // * Unity3dRider is installed
            //   Uses Unity's own generation and adds correct `LangVersion`
            //   `TargetFrameworkVersion` will be `v3.5` or `v4.6`
            // * CSharp60Support is installed
            //   .csproj has NO `LangVersion`
            //   `TargetFrameworkVersion` is NOT accurate (support for C# 6 is not dependent on/trigger by .net 4.6)
            //   Look for `CSharp60Support` or `CSharp70Support` folders
            //
            // Actions:
            // If `LangVersion` is missing or "default"
            // then override based on `TargetFrameworkVersion` or presence of `CSharp60Support`/`CSharp70Support`
            // else do nothing
            //
            // Notes:
            // * Unity and VSTU have two separate .csproj routines. VSTU adds extra references,
            //   the VSTU project flavour GUID and imports UnityVs.targets, which disables the
            //   `GenerateTargetFrameworkMonikerAttribute` target
            // * CSharp60Support post-processes the .csproj file direclty if VSTU is not installed.
            //   If it is installed, it registers a delegate with `ProjectFilesGenerator.ProjectFileGeneration`
            //   and removes it before it's written to disk
            // * `LangVersion` can be conditionally specified, which makes checking for "default" awkward
            // * If Unity3dRider + CSharp60Support are both installed, last write wins
            //   Order of post-processing is non-deterministic, so Rider's LangVersion might be removed
            // * Unity3dRider can set `TargetFrameworkVersion` to `v4.5` on non-Windows machines to fix
            //   an issue resolving System.Linq

            var languageLevel = CSharpLanguageLevel.Default;
            if (IsLangVersionMissing(project) || IsLangVersionDefault(project))
            {
#if WAVE07
                const CSharpLanguageLevel csharp70 = CSharpLanguageLevel.CSharp70;
#else
                const CSharpLanguageLevel csharp70 = CSharpLanguageLevel.Experimental;
#endif

                // Support for https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration
                // See also https://github.com/JetBrains/resharper-unity/issues/50#issuecomment-257611218
                if (project.Location.CombineWithShortName("CSharp70Support").ExistsDirectory)
                    languageLevel = csharp70;
                else if (project.Location.CombineWithShortName("CSharp60Support").ExistsDirectory)
                    languageLevel = CSharpLanguageLevel.CSharp60;
                else
                {
                    languageLevel = IsTargetFrameworkAtLeast46(project)
                        ? CSharpLanguageLevel.CSharp60
                        : CSharpLanguageLevel.CSharp40;
                }
            }
            SetValue(mountPoint, (CSharpLanguageProjectSettings s) => s.LanguageLevel, languageLevel);
        }

        private bool IsLangVersionMissing(IProject project)
        {
            return !myLangVersionCache.IsLangVersionExplicitlySpecified(project);
        }

        private bool IsLangVersionDefault(IProject project)
        {
            // VSTU sets LangVersion to default. Would make life so much
            // easier if it just specified the actual language version.
            // This is stored per-configuration, gotta check 'em all
            foreach (var configuration in project.ProjectProperties.ActiveConfigurations.Configurations)
            {
                var csharpConfiguration = configuration as ICSharpProjectConfiguration;
                if (csharpConfiguration != null)
                {
                    if (csharpConfiguration.LanguageVersion != VSCSharpLanguageVersion.Latest)
                        return false;
                }
            }
            return true;
        }

        private bool IsTargetFrameworkAtLeast46(IProject project)
        {
            // ReSharper disable once PossibleNullReferenceException (never null for real project)
            return project.PlatformID.Version >= Version46;
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