using System;
using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Packages;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
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
        private readonly JetHashSet<VirtualFileSystemPath> myNotificationShown;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
        private string packageId = "com.unity.ide.rider";

        public RiderPackageUpdateAvailabilityChecker(
            Lifetime lifetime,
            ILogger logger,
            NotificationsModel notifications,
            ISolution solution,
            IShellLocks shellLocks,
            PackageManager packageManager,
            UnitySolutionTracker unitySolutionTracker,
            UnityVersion unityVersion,
            IApplicationWideContextBoundSettingStore settingsStore)
        {
            myLogger = logger;
            myNotifications = notifications;
            mySolution = solution;
            myShellLocks = shellLocks;
            myPackageManager = packageManager;
            myUnityVersion = unityVersion;
            myNotificationShown = new JetHashSet<VirtualFileSystemPath>();
            myBoundSettingsStore = settingsStore.BoundSettingsStore;
            unitySolutionTracker.IsUnityProjectFolder.WhenTrue(lifetime, lt =>
            {
                ShowNotificationIfNeeded(lt);
                BindToInstallationSettingChange(lt);
            });
        }
        
        private void BindToInstallationSettingChange(Lifetime lifetime)
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.AllowRiderUpdateNotifications);
            myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Change.Advise_NoAcknowledgement(lifetime, args =>
            {
                if (!args.GetNewOrNull()) return;
                ShowNotificationIfNeeded(lifetime);
            });
        }

        private void ShowNotificationIfNeeded(Lifetime lifetime)
        {
            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.AllowRiderUpdateNotifications))
                return;
            
            myPackageManager.IsInitialUpdateFinished.WhenTrue(lifetime, lt =>
            {
                var version = myUnityVersion.ActualVersionForSolution.Value;
                if (version == null)
                    return;
            
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
                    var notification = new NotificationModel($"JetBrains Rider package in Unity is missing.", "Make sure JetBrains Rider package is installed in Unity Package Manager.", true, RdNotificationEntryType.WARN, new List<NotificationHyperlink>());
                    myShellLocks.ExecuteOrQueueEx(lifetime, "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded", () => myNotifications.Notification(notification));
                }
                // todo: read available newest compatible Rider package version from somewhere, like protocol or json file
                else if (package.Source == PackageSource.Registry && new Version(package.PackageDetails.Version) < new Version(3,0,7))
                {
                    myNotificationShown.Add(mySolution.SolutionFilePath);
                    myLogger.Info($"{packageId} {package.PackageDetails.Version} is older then expected.");
                    var notification = new NotificationModel($"Update available - JetBrains Rider package.", "Check for JetBrains Rider package updates in Unity Package Manager.", true, RdNotificationEntryType.INFO, new List<NotificationHyperlink>());
                    myShellLocks.ExecuteOrQueueEx(lifetime, "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded", () => myNotifications.Notification(notification));
                }
            } );
        }
    }
}