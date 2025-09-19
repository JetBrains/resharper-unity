using System;
using System.Collections.Generic;
using JetBrains.Application.Notifications;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages.Notification
{
    [SolutionComponent(Instantiation.ContainerAsyncAnyThreadSafe)]
    public class RiderPackageUpdateAvailabilityChecker
    {
        private readonly ILogger myLogger;
        private readonly ISolution mySolution;
        private readonly IShellLocks myShellLocks;
        private readonly PackageManager myPackageManager;
        private readonly UnityVersion myUnityVersion;
        private readonly ISettingsStore mySettingsStore;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly UserNotifications myUserNotifications;
        private readonly FrontendBackendHost myFrontendBackendHost;
        private readonly SequentialLifetimes mySequentialLifetimes;
        private readonly JetHashSet<JetSemanticVersion> myNotificationShown;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
        private string packageId = PackageCompatibilityValidator.RiderPackageId;
        private JetSemanticVersion leastRiderPackageVersion = new(3, 0, 38);

        public RiderPackageUpdateAvailabilityChecker(
            Lifetime lifetime,
            ILogger logger,
            ISolution solution,
            IShellLocks shellLocks,
            PackageManager packageManager,
            UnitySolutionTracker unitySolutionTracker,
            UnityVersion unityVersion,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            ISettingsStore settingsStore,
            BackendUnityHost backendUnityHost, 
            UserNotifications userNotifications,
            FrontendBackendHost frontendBackendHost
        )
        {
            myLogger = logger;
            mySolution = solution;
            myShellLocks = shellLocks;
            myPackageManager = packageManager;
            myUnityVersion = unityVersion;
            mySettingsStore = settingsStore;
            myBackendUnityHost = backendUnityHost;
            myUserNotifications = userNotifications;
            myFrontendBackendHost = frontendBackendHost;
            mySequentialLifetimes = new SequentialLifetimes(lifetime);
            myNotificationShown = new JetHashSet<JetSemanticVersion>();
            myBoundSettingsStore = applicationWideContextBoundSettingStore.BoundSettingsStore;
            unitySolutionTracker.IsUnityProject.WhenTrue(lifetime, lt =>
            {
                ShowNotificationIfNeeded(lt, leastRiderPackageVersion);
                BindToInstallationSettingChange(lt, leastRiderPackageVersion);
                BindToProtocol(lt);
            });
        }

        private void BindToProtocol(Lifetime lt)
        {
            myBackendUnityHost.BackendUnityModel.ViewNotNull(lt, (l, model) =>
            {
                model.RiderPackagePotentialUpdateVersion.Advise(l, result =>
                {
                    if (!string.IsNullOrEmpty(result) && JetSemanticVersion.TryParse(result, out var resultVersion))
                    {
                        ShowNotificationIfNeeded(l, resultVersion);
                    }
                });
            });
        }

        private void BindToInstallationSettingChange(Lifetime lifetime, JetSemanticVersion version)
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) =>
                s.AllowRiderUpdateNotifications);
            myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise_NoAcknowledgement(lifetime,
                args =>
                {
                    if (!args.IsRaising()) return;
                    ShowNotificationIfNeeded(lifetime, version);
                });
        }

        private void ShowNotificationIfNeeded(Lifetime lifetime, JetSemanticVersion packageVersion)
        {
            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowRiderUpdateNotifications))
                return;

            myPackageManager.IsInitialUpdateFinished.WhenTrue(lifetime, lt =>
            {
                myUnityVersion.ActualVersionForSolution.AdviseNotNull(lt, unityVersion =>
                {
                    if (myNotificationShown.Contains(packageVersion)) return;

                    // Version before 2019.2 doesn't have Rider package
                    // 2019.2.0 - 2019.2.5 : version 1.2.1 is the last one
                    // 2019.2.6 - last one is 3.0.33
                    // 2019.4 - last one is 3.0.36
                    // 2021.3 - 3.0.37+
                    if (unityVersion < new Version(2021, 3, 0)) return;
                    var notificationLifetime = mySequentialLifetimes.Next().CreateNested(); // avoid multiple notifications simultaneously

                    var package = myPackageManager.GetPackageById(packageId);

                    if (package == null)
                    {
                        myNotificationShown.Add(packageVersion);
                        myLogger.Info($"{packageId} is missing.");
                        myShellLocks.ExecuteOrQueueEx(notificationLifetime.Lifetime,
                            "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                            () =>
                            {
                                myUserNotifications.CreateNotification(notificationLifetime.Lifetime, NotificationSeverity.WARNING,
                                    Strings.RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_JetBrains_Rider_package_in_Unity_is_missing_,
                                    Strings.RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Make_sure_JetBrains_Rider_package_is_installed_in_Unity_Package_Manager_);
                            });
                    }
                    else
                    {
                        var packageStringCurrentVersion = package.PackageDetails.Version;
                        var isCurrentVersionParsed = JetSemanticVersion.TryParse(packageStringCurrentVersion, out var currentPackageVersion);

                        // for local or git packages version would not get parsed
                        Assertion.Assert(!(package.Source == PackageSource.Registry && !isCurrentVersionParsed), "JetSemanticVersion.TryParse returned false for package version {0}, package Id: {1}", packageStringCurrentVersion, package.Id);
                        
                        // RSRP-496055 JetSemanticVersion equality problem
                        if (isCurrentVersionParsed && package.Source == PackageSource.Registry && (currentPackageVersion != packageVersion && currentPackageVersion < packageVersion))
                        {
                            myNotificationShown.Add(packageVersion);
                            myLogger.Info($"{packageId} {packageStringCurrentVersion} is older then expected.");

                            myShellLocks.ExecuteOrQueueEx(notificationLifetime.Lifetime,
                                "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                                () => myUserNotifications.CreateNotification(notificationLifetime.Lifetime,
                                    NotificationSeverity.INFO,
                                    Resources.Strings.RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Update_available___JetBrains_Rider_package_,
                                    string.Format(Resources.Strings.RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__Version__in_Unity_Package_Manager_, packageVersion),
                                    additionalCommands: new[]
                                    {
                                        new UserNotificationCommand(Resources.Strings.OpenManifestJson_Text, () =>
                                        {
                                            var model = myFrontendBackendHost.Model;
                                            var file = mySolution.SolutionDirectory.Combine("Packages").Combine("manifest.json");
                                            if (model != null && file.ExistsFile)
                                            { 
                                                model.OpenFileLineCol.Start(mySolution.GetSolutionLifetimes().UntilSolutionCloseLifetime, new RdOpenFileArgs(file.FullPath, 1, 0));
                                                notificationLifetime.Terminate();
                                            }
                                        }),
                                        new UserNotificationCommand(Resources.Strings.RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Do_not_show_for_this_solution, () =>
                                        {
                                            mySettingsStore.BindToContextTransient(
                                                    ContextRange.ManuallyRestrictWritesToOneContext(
                                                        mySolution.ToDataContext()))
                                                .SetValue((UnitySettings key) => key.AllowRiderUpdateNotifications, false);
                                            notificationLifetime.Terminate();
                                        })
                                    }));
                        }
                    }
                });
            });
        }
    }
} 