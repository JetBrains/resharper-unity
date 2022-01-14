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
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Backend.Features.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnityEditorIntegration
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
        private readonly RiderNotificationPopupHost myNotificationPopupHost;
        private readonly JetHashSet<VirtualFileSystemPath> myNotificationShown;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
        private string packageId = "com.unity.ide.rider";
        private Version leastRiderPackageVersion = new Version(3, 0, 9);

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
            RiderNotificationPopupHost notificationPopupHost
        )
        {
            myLogger = logger;
            mySolution = solution;
            myShellLocks = shellLocks;
            myPackageManager = packageManager;
            myUnityVersion = unityVersion;
            mySettingsStore = settingsStore;
            myBackendUnityHost = backendUnityHost;
            myNotificationPopupHost = notificationPopupHost;
            myNotificationShown = new JetHashSet<VirtualFileSystemPath>();
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
                    // 2019.2.6 - present : see: leastRiderPackageVersion
                    if (version < new Version(2019, 2, 6)) return;

                    var package = myPackageManager.GetPackageById(packageId);

                    if (package == null)
                    {
                        myNotificationShown.Add(mySolution.SolutionFilePath);
                        myLogger.Info($"{packageId} is missing.");
                        var notification = RiderNotification.Create(NotificationSeverity.WARNING,
                            "JetBrains Rider package in Unity is missing.",
                            "Make sure JetBrains Rider package is installed in Unity Package Manager."
                        );
                        myShellLocks.ExecuteOrQueueEx(lt,
                            "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                            () =>
                            {
                                myNotificationPopupHost.ShowNotification(lt, notification);
                            });
                    }
                    else if (package.Source == PackageSource.Registry &&
                             new Version(package.PackageDetails.Version) < expectedVersion)
                    {
                        var notificationLifetime = lt.CreateNested();
                        myNotificationShown.Add(mySolution.SolutionFilePath);
                        myLogger.Info($"{packageId} {package.PackageDetails.Version} is older then expected.");
                        var notification = RiderNotification.Create(NotificationSeverity.INFO,
                            "Update available - JetBrains Rider package.",
                            "Check for JetBrains Rider package updates in Unity Package Manager.",
                            additionalCommands: new[]
                            {
                                new UserNotificationCommand("Never show for this solution", () =>
                                {
                                    mySettingsStore.BindToContextTransient(
                                            ContextRange.ManuallyRestrictWritesToOneContext(mySolution.ToDataContext()))
                                        .SetValue((UnitySettings key) => key.AllowRiderUpdateNotifications, false);
                                    notificationLifetime.Terminate();
                                })
                            }
                        );

                        myShellLocks.ExecuteOrQueueEx(lt,
                            "RiderPackageUpdateAvailabilityChecker.ShowNotificationIfNeeded",
                            () => myNotificationPopupHost.ShowNotification(notificationLifetime.Lifetime, notification));
                    }
                });
            });
        }
    }
} 