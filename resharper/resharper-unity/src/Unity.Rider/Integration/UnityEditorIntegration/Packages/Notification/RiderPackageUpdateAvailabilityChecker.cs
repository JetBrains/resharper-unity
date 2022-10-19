using System;
using System.Collections.Generic;
using JetBrains.Application.Notifications;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
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

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.Packages.Notification
{
    [SolutionComponent]
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
        private readonly SequentialLifetimes mySequentialLifetimes;
        private readonly JetHashSet<Version> myNotificationShown;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
        private string packageId = PackageCompatibilityValidator.RiderPackageId;
        private Version leastRiderPackageVersion = new Version(3, 0, 16);

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
            UserNotifications userNotifications
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
            mySequentialLifetimes = new SequentialLifetimes(lifetime);
            myNotificationShown = new JetHashSet<Version>();
            myBoundSettingsStore = applicationWideContextBoundSettingStore.BoundSettingsStore;
            unitySolutionTracker.IsUnityGeneratedProject.WhenTrue(lifetime, lt =>
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
                    if (!string.IsNullOrEmpty(result) && Version.TryParse(result, out var resultVersion))
                    {
                        ShowNotificationIfNeeded(l, resultVersion);
                    }
                });
            });
        }

        private void BindToInstallationSettingChange(Lifetime lifetime, Version version)
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) =>
                s.AllowRiderUpdateNotifications);
            myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise_NoAcknowledgement(lifetime,
                args =>
                {
                    if (!args.GetNewOrNull()) return;
                    ShowNotificationIfNeeded(lifetime, version);
                });
        }

        private void ShowNotificationIfNeeded(Lifetime lifetime, Version packageVersion)
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
                    // 2019.2.6 - present : see: leastRiderPackageVersion
                    if (unityVersion < new Version(2019, 2, 6)) return;
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
                                    "JetBrains Rider package in Unity is missing.",
                                    "Make sure JetBrains Rider package is installed in Unity Package Manager.");
                            });
                    }
                    else if (package.Source == PackageSource.Registry &&
                             new Version(package.PackageDetails.Version) < packageVersion)
                    {
                        myNotificationShown.Add(packageVersion);
                        myLogger.Info($"{packageId} {package.PackageDetails.Version} is older then expected.");

                        myShellLocks.ExecuteOrQueueEx(notificationLifetime.Lifetime,
                            "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                            () => myUserNotifications.CreateNotification(notificationLifetime.Lifetime,
                                NotificationSeverity.INFO,
                                "Update available - JetBrains Rider package.",
                                $"Check for JetBrains Rider package {packageVersion} in Unity Package Manager.",
                                additionalCommands: new[]
                                {
                                    new UserNotificationCommand("Never show for this solution", () =>
                                    {
                                        mySettingsStore.BindToContextTransient(
                                                ContextRange.ManuallyRestrictWritesToOneContext(
                                                    mySolution.ToDataContext()))
                                            .SetValue((UnitySettings key) => key.AllowRiderUpdateNotifications, false);
                                        notificationLifetime.Terminate();
                                    })
                                }));
                    }
                });
            });
        }
    }
} 