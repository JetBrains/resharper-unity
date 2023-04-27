using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Notifications;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Util;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.Application.I18n;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.EditorPlugin
{
    [SolutionComponent]
    public class UnityPluginInstaller
    {
        private readonly JetHashSet<VirtualFileSystemPath> myPluginInstallations;
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly IShellLocks myShellLocks;
        private readonly UnityPluginDetector myDetector;
        private readonly ILogger myLogger;
        private readonly NotificationsModel myNotifications;
        private readonly PluginPathsProvider myPluginPathsProvider;
        private readonly UnityVersion myUnityVersion;
        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly UnityRefresher myRefresher;
        private readonly UserNotifications myUserNotifications;
        private readonly BackendUnityProtocol myBackendUnityProtocol;
        private readonly IHostProductInfo myHostProductInfo;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;
        private readonly ProcessingQueue myQueue;

        public UnityPluginInstaller(
            Lifetime lifetime,
            ILogger logger,
            ISolution solution,
            IShellLocks shellLocks,
            UnityPluginDetector detector,
            NotificationsModel notifications,
            IApplicationWideContextBoundSettingStore settingsStore,
            PluginPathsProvider pluginPathsProvider,
            UnityVersion unityVersion,
            FrontendBackendHost frontendBackendHost,
            UnitySolutionTracker unitySolutionTracker,
            UnityRefresher refresher,
            IHostProductInfo hostProductInfo,
            UserNotifications userNotifications, 
            BackendUnityProtocol backendUnityProtocol)
        {
            myPluginInstallations = new JetHashSet<VirtualFileSystemPath>();

            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myShellLocks = shellLocks;
            myDetector = detector;
            myNotifications = notifications;
            myPluginPathsProvider = pluginPathsProvider;
            myUnityVersion = unityVersion;
            myUnitySolutionTracker = unitySolutionTracker;
            myRefresher = refresher;
            myHostProductInfo = hostProductInfo;
            myUserNotifications = userNotifications;
            myBackendUnityProtocol = backendUnityProtocol;

            myBoundSettingsStore = settingsStore.BoundSettingsStore;
            myQueue = new ProcessingQueue(myShellLocks, myLifetime);

            frontendBackendHost.Do(frontendBackendModel =>
            {
                frontendBackendModel.InstallEditorPlugin.AdviseNotNull(lifetime, _ =>
                {
                    myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.InstallEditorPlugin", () =>
                    {
                        var installationInfo = myDetector.GetInstallationInfo(myCurrentVersion);
                        QueueInstall(installationInfo, true);
                    });
                });
            });

            unitySolutionTracker.IsUnityProjectFolder.AdviseOnce(lifetime, args =>
            {
                if (!args) return;

                var versionForSolution = myUnityVersion.ActualVersionForSolution.Value;
                if (versionForSolution.Major >= 2019) // it is very unlikely that project is upgraded from pre-2019.2 to 2020.x+
                    return;

                shellLocks.Tasks.StartNew(lifetime, Scheduling.FreeThreaded, () =>
                {
                    var info = myDetector.GetInstallationInfo(myCurrentVersion,
                        previousInstallationDir: VirtualFileSystemPath.GetEmptyPathFor(InteractionContext
                            .SolutionContext));

                    myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "IsAbleToEstablishProtocolConnectionWithUnity",
                        () => InstallPluginIfRequired(info));
                    BindToInstallationSettingChange(info);
                    return Task.CompletedTask;
                }).NoAwait();
            });
            
            myBackendUnityProtocol.OutOfSync.Advise(lifetime, ShowOutOfSyncNotification);
        }

        private void BindToInstallationSettingChange(UnityPluginDetector.InstallationInfo info)
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
            myBoundSettingsStore.GetValueProperty<bool>(myLifetime, entry, null).Change.Advise_NoAcknowledgement(myLifetime, args =>
            {
                if (!args.GetNewOrNull()) return;
                myShellLocks.ExecuteOrQueueReadLockEx(myLifetime,
                    "UnityPluginInstaller.CheckAllProjectsIfAutoInstallEnabled", () => InstallPluginIfRequired(info));
            });
        }

        readonly Version myCurrentVersion = typeof(UnityPluginInstaller).Assembly.GetName().Version;

        private void InstallPluginIfRequired(UnityPluginDetector.InstallationInfo installationInfo)
        {
            if (!myUnitySolutionTracker.IsUnityProjectFolder.Value)
                return;

            if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                return;

            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.InstallUnity3DRiderPlugin))
                return;

            var versionForSolution = myUnityVersion.ActualVersionForSolution.Value;
            if (versionForSolution >= new Version("2019.2")) // 2019.2+ would not work fine either without Rider package, and when package is present it loads EditorPlugin directly from Rider installation.
            {
                if (!installationInfo.PluginDirectory.IsAbsolute)
                    return;

                var pluginDll = installationInfo.PluginDirectory.Combine(PluginPathsProvider.BasicPluginDllFile);
                if (pluginDll.ExistsFile)
                {
                    myQueue.Enqueue(() =>
                    {
                        myLogger.Info($"Remove {pluginDll}. Rider package should be used instead.");
                        pluginDll.DeleteFile();
                        VirtualFileSystemPath.Parse(pluginDll.FullPath + ".meta", InteractionContext.SolutionContext).DeleteFile();

                        // jetbrainsDir is usually "Assets\Plugins\Editor\JetBrains", however custom locations were also possible
                        var jetbrainsDir = installationInfo.PluginDirectory;
                        if (jetbrainsDir.GetChildren().Any() || jetbrainsDir.Name != "JetBrains") return;
                        jetbrainsDir.DeleteDirectoryNonRecursive();
                        VirtualFileSystemPath.Parse(jetbrainsDir.FullPath + ".meta", InteractionContext.SolutionContext).DeleteFile();
                        var pluginsEditorDir = jetbrainsDir.Directory;
                        if (pluginsEditorDir.GetChildren().Any() || pluginsEditorDir.Name != "Editor") return;
                        pluginsEditorDir.DeleteDirectoryNonRecursive();
                        VirtualFileSystemPath.Parse(pluginsEditorDir.FullPath + ".meta", InteractionContext.SolutionContext).DeleteFile();
                        var pluginsDir = pluginsEditorDir.Directory;
                        if (pluginsDir.GetChildren().Any() || pluginsDir.Name != "Plugins") return;
                        pluginsDir.DeleteDirectoryNonRecursive();
                        VirtualFileSystemPath.Parse(pluginsDir.FullPath+ ".meta", InteractionContext.SolutionContext).DeleteFile();
                    });
                }
                return;
            }

            // forcing fresh install due to being unable to provide proper setting until InputField is patched in Rider
            // ReSharper disable once ArgumentsStyleNamedExpression
            if (!installationInfo.ShouldInstallPlugin)
            {
                myLogger.Info("Plugin should not be installed.");
                if (installationInfo.ExistingFiles.Count > 0)
                    myLogger.Info("Already existing plugin files:\n{0}",
                        string.Join("\n", installationInfo.ExistingFiles));

                return;
            }

            QueueInstall(installationInfo);
            myQueue.Enqueue(() =>
            {
                mySolution.Locks.Tasks.StartNew(myLifetime, Scheduling.MainGuard,
                    () => myRefresher.StartRefresh(RefreshType.Normal));
            });
        }

        private void QueueInstall(UnityPluginDetector.InstallationInfo installationInfo, bool force = false)
        {
            myQueue.Enqueue(() =>
            {
                Install(installationInfo, force);
                myPluginInstallations.Add(mySolution.SolutionFilePath);
            });
        }

        private void Install(UnityPluginDetector.InstallationInfo installationInfo, bool force)
        {
            if(!myPluginPathsProvider.ShouldRunInstallation())
            {
                myLogger.Info("Skip Install, because myPluginPathsProvider.ShouldRunInstallation returned false.");
                return;
            }
            
            if (!force)
            {
                if (!installationInfo.ShouldInstallPlugin)
                {
                    Assertion.Assert(false, "Should not be here if installation is not required.");
                    return;
                }

                if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                {
                    myLogger.Verbose("Installation already done.");
                    return;
                }
            }

            myLogger.Info("Installing Rider Unity editor plugin: {0}", installationInfo.InstallReason);

            if (!TryCopyFiles(installationInfo, out var installedPath))
            {
                myLogger.Warn("Plugin was not installed");
            }
            else
            {
                string userTitle;
                string userMessage;

                switch (installationInfo.InstallReason)
                {
                    case UnityPluginDetector.InstallReason.FreshInstall:
                        userTitle = Strings.UnityEditorPluginInstalled_Text;
                        userMessage = Strings.PleaseSwitchToUnityEditorToLoadThePlugin_Text.Format(myCurrentVersion, installedPath.MakeRelativeTo(mySolution.SolutionDirectory));
                        break;

                    case UnityPluginDetector.InstallReason.Update:
                        userTitle = Strings.UnityEditorPluginUpdated_Text;
                        userMessage = Strings.PleaseSwitchToTheUnityEditorToReload_Text.Format(myCurrentVersion, installedPath.MakeRelativeTo(mySolution.SolutionDirectory));
                        break;

                    case UnityPluginDetector.InstallReason.ForceUpdateForDebug:
                        userTitle = Strings.UnityEditorPluginUpdatedDebugBuild_Text;
                        userMessage = Strings.PleaseSwitchToTheUnityEditorToReload_Text.Format(myCurrentVersion, installedPath.MakeRelativeTo(mySolution.SolutionDirectory));
                        break;

                    case UnityPluginDetector.InstallReason.UpToDate:
                        userTitle = Strings.UnityEditorPluginUpdatedUpToDate_Text;
                        userMessage = Strings.PleaseSwitchToTheUnityEditorToReload_Text.Format(myCurrentVersion, installedPath.MakeRelativeTo(mySolution.SolutionDirectory));
                        break;

                    default:
                        myLogger.Error("Unexpected install reason: {0}", installationInfo.InstallReason);
                        return;
                }

                myLogger.Info(userTitle);

                var notification = new NotificationModel(userTitle, userMessage, true, RdNotificationEntryType.INFO, new List<NotificationHyperlink>());

                myShellLocks.ExecuteOrQueueEx(myLifetime, "UnityPluginInstaller.Notify", () => myNotifications.Notification(notification));
            }
        }

        public bool TryCopyFiles([NotNull] UnityPluginDetector.InstallationInfo installation, out VirtualFileSystemPath installedPath)
        {
            installedPath = null;
            try
            {
                installation.PluginDirectory.CreateDirectory();

                return DoCopyFiles(installation, out installedPath);
            }
            catch (Exception e)
            {
                myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.OuterWorld, "Plugin installation failed");
                return false;
            }
        }

        private bool DoCopyFiles([NotNull] UnityPluginDetector.InstallationInfo installation, out VirtualFileSystemPath installedPath)
        {
            installedPath = null;

            var originPaths = new List<VirtualFileSystemPath>();
            originPaths.AddRange(installation.ExistingFiles);

            var backups = originPaths.ToDictionary(f => f, f => f.AddSuffix(".backup"));

            foreach (var originPath in originPaths)
            {
                var backupPath = backups[originPath];
                if (originPath.ExistsFile)
                {
                    originPath.MoveFile(backupPath, true);
                    myLogger.Info($"backing up: {originPath.Name} -> {backupPath.Name}");
                }
                else
                    myLogger.Info($"backing up failed: {originPath.Name} doesn't exist.");
            }

            try
            {
                var editorPluginPathDir = myPluginPathsProvider.GetEditorPluginPathDir();
                var editorPluginPath = editorPluginPathDir.Combine(PluginPathsProvider.BasicPluginDllFile);
                var editor56PluginPath = editorPluginPathDir.Combine(PluginPathsProvider.Unity56PluginDllFile);
                var editorFullPluginPath = editorPluginPathDir.Combine(PluginPathsProvider.FullPluginDllFile);

                var targetPath = installation.PluginDirectory.Combine(editorPluginPath.Name);
                try
                {
                    var versionForSolution = myUnityVersion.ActualVersionForSolution.Value;
                    if (versionForSolution < new Version("5.6"))
                    {
                        myLogger.Verbose($"Coping {editorPluginPath} -> {targetPath}");
                        editorPluginPath.CopyFile(targetPath, true);
                    }
                    else if (versionForSolution >= new Version("5.6") && versionForSolution < new Version("2017.3"))
                    {
                        myLogger.Verbose($"Coping {editor56PluginPath} -> {editor56PluginPath}");
                        editor56PluginPath.CopyFile(targetPath, true);
                    }
                    else
                    {
                        myLogger.Verbose($"Coping {editorFullPluginPath} -> {targetPath}");
                        editorFullPluginPath.CopyFile(targetPath, true);
                    }
                }
                catch (Exception e)
                {
                    myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.Assertion,
                        $"Failed to copy {editorPluginPath} => {targetPath}");
                    RestoreFromBackup(backups);
                }

                foreach (var backup in backups)
                {
                    backup.Value.DeleteFile();
                }

                installedPath = installation.PluginDirectory.Combine(PluginPathsProvider.BasicPluginDllFile);
                return true;
            }
            catch (Exception e)
            {
                myLogger.LogExceptionSilently(e);

                RestoreFromBackup(backups);

                return false;
            }
        }

        private void RestoreFromBackup(Dictionary<VirtualFileSystemPath, VirtualFileSystemPath> backups)
        {
            foreach (var backup in backups)
            {
                myLogger.Info($"Restoring from backup {backup.Value} -> {backup.Key}");
                backup.Value.MoveFile(backup.Key, true);
            }
        }
        
        internal void ShowOutOfSyncNotification(Lifetime lifetime)
        {
            var notificationLifetime = lifetime.CreateNested();
            var appVersion = myUnityVersion.ActualVersionForSolution.Value;
            if (appVersion < new Version(2019, 2))
            {
                var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
                var isEnabled = myBoundSettingsStore.GetValueProperty<bool>(lifetime, entry, null).Value;
                if (isEnabled) return;
                myUserNotifications.CreateNotification(notificationLifetime.Lifetime, NotificationSeverity.WARNING, Strings.UnityEditorPluginUpdateRequired_Text,
                    Strings.TheUnityEditorPluginIsOutOfDateAndAutomatic_Text,
                    additionalCommands: new[]
                    {
                        new UserNotificationCommand(Strings.DoNotShowForThisSolution_Text, () =>
                        {
                            mySolution.Locks.ExecuteOrQueueReadLockEx(notificationLifetime.Lifetime,
                                "UnityPluginInstaller.InstallEditorPlugin", () =>
                                {
                                    var installationInfo = myDetector.GetInstallationInfo(myCurrentVersion);
                                    QueueInstall(installationInfo, true);
                                    notificationLifetime.Terminate();
                                });
                        })
                    });
            }
            else
            {
                myUserNotifications.CreateNotification(notificationLifetime.Lifetime, NotificationSeverity.WARNING,
                    Strings.AdvancedUnityIntegrationIsUnavailable_Text,
                    Strings.MakeSureRider_IsSetAsTheExternalEditor_Text.Format(myHostProductInfo.VersionMarketingString));
            }
        }
    }
}