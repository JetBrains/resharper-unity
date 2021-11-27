using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Packages;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class RiderPackageUpdateAvailabilityChecker
    {
        private readonly ILogger myLogger;
        private readonly NotificationsModel myNotifications;
        private readonly ISolution mySolution;
        private readonly IShellLocks myShellLocks;
        private readonly PackageManager myPackageManager;
        private readonly UnityVersion myUnityVersion;
        private readonly ISettingsStore mySettingsStore;
        private readonly BackendUnityHost myBackendUnityHost;
        private readonly JetHashSet<VirtualFileSystemPath> myNotificationShown;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
        private string packageId = "com.unity.ide.rider";
        private string myHyperlinkId = "NeverShowRiderPackageUpdateAvailable";

        public RiderPackageUpdateAvailabilityChecker(
            Lifetime lifetime,
            ILogger logger,
            NotificationsModel notifications,
            ISolution solution,
            IShellLocks shellLocks,
            PackageManager packageManager,
            UnitySolutionTracker unitySolutionTracker,
            UnityVersion unityVersion,
            IApplicationWideContextBoundSettingStore applicationWideContextBoundSettingStore,
            ISettingsStore settingsStore,
            BackendUnityHost backendUnityHost
        )
        {
            myLogger = logger;
            myNotifications = notifications;
            mySolution = solution;
            myShellLocks = shellLocks;
            myPackageManager = packageManager;
            myUnityVersion = unityVersion;
            mySettingsStore = settingsStore;
            myBackendUnityHost = backendUnityHost;
            myNotificationShown = new JetHashSet<VirtualFileSystemPath>();
            myBoundSettingsStore = applicationWideContextBoundSettingStore.BoundSettingsStore;
            unitySolutionTracker.IsUnityProjectFolder.WhenTrue(lifetime, lt =>
            {
                ShowNotificationIfNeeded(lt, new Version(3, 0, 7));
                BindToInstallationSettingChange(lt, new Version(3, 0, 7));
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

        private void ShowNotificationIfNeeded(Lifetime lifetime, Version expectedVersion)
        {
            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowRiderUpdateNotifications))
                return;

            myPackageManager.IsInitialUpdateFinished.WhenTrue(lifetime, lt =>
            {
                myUnityVersion.ActualVersionForSolution.AdviseNotNull(lt, version =>
                {
                    if (myNotificationShown.Contains(mySolution.SolutionFilePath)) return;

                    // Version before 2019.2 doesn't have Rider package
                    // 2019.2.0 - 2019.2.5 : version 1.2.1 is the last one
                    // 2019.2.6 - present : 3.0.7+
                    if (version < new Version(2019, 2, 6)) return;

                    var package = myPackageManager.GetPackageById(packageId);

                    if (package == null)
                    {
                        myNotificationShown.Add(mySolution.SolutionFilePath);
                        myLogger.Info($"{packageId} is missing.");
                        var notification = new NotificationModel($"JetBrains Rider package in Unity is missing.",
                            "Make sure JetBrains Rider package is installed in Unity Package Manager.", true,
                            RdNotificationEntryType.WARN, new List<NotificationHyperlink>());
                        myShellLocks.ExecuteOrQueueEx(lt,
                            "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                            () => myNotifications.Notification(notification));
                    }
                    else if (package.Source == PackageSource.Registry &&
                             new Version(package.PackageDetails.Version) < expectedVersion)
                    {
                        myNotificationShown.Add(mySolution.SolutionFilePath);
                        myLogger.Info($"{packageId} {package.PackageDetails.Version} is older then expected.");
                        var neverShowLink = new NotificationHyperlink("Never show for this solution", myHyperlinkId);
                        var notificationId = new Random().Next();
                        var notification = new NotificationModel("Update available - JetBrains Rider package.",
                            "Check for JetBrains Rider package updates in Unity Package Manager.",
                            true, RdNotificationEntryType.INFO, new List<NotificationHyperlink> { neverShowLink },
                            notificationId);
                        var notificationLifetime = lt.CreateNested();
                        myNotifications.ExecuteHyperlink.Advise(notificationLifetime.Lifetime, id =>
                        {
                            if (id == myHyperlinkId)
                            {
                                mySettingsStore.BindToContextTransient(
                                        ContextRange.ManuallyRestrictWritesToOneContext(mySolution.ToDataContext()))
                                    .SetValue((UnitySettings key) => key.AllowRiderUpdateNotifications, false);
                                myNotifications.NotificationExpired.Fire(notificationId);
                                notificationLifetime.Terminate();
                            }
                        });

                        myShellLocks.ExecuteOrQueueEx(notificationLifetime.Lifetime,
                            "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                            () => myNotifications.Notification(notification));
                    }
                });
            });
        }
    }
}