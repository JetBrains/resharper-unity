﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginInstaller
    {
        private readonly JetHashSet<FileSystemPath> myPluginInstallations;
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
            UnityRefresher refresher)
        {
            myPluginInstallations = new JetHashSet<FileSystemPath>();

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

            myBoundSettingsStore = settingsStore.BoundSettingsStore;
            myQueue = new ProcessingQueue(myShellLocks, myLifetime);

            frontendBackendHost.Do(frontendBackendModel =>
            {
                frontendBackendModel.InstallEditorPlugin.AdviseNotNull(lifetime, x =>
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
                myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "IsAbleToEstablishProtocolConnectionWithUnity", InstallPluginIfRequired);
                BindToInstallationSettingChange();
            });
        }

        private void BindToInstallationSettingChange()
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
            myBoundSettingsStore.GetValueProperty<bool>(myLifetime, entry, null).Change.Advise_NoAcknowledgement(myLifetime, args =>
            {
                if (!args.GetNewOrNull()) return;
                myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.CheckAllProjectsIfAutoInstallEnabled", InstallPluginIfRequired);
            });
        }

        readonly Version myCurrentVersion = typeof(UnityPluginInstaller).Assembly.GetName().Version;

        private void InstallPluginIfRequired()
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
                var installationInfoToRemove = myDetector.GetInstallationInfo(myCurrentVersion, previousInstallationDir: FileSystemPath.Empty);
                if (!installationInfoToRemove.PluginDirectory.IsAbsolute)
                    return;

                var pluginDll = installationInfoToRemove.PluginDirectory.Combine(PluginPathsProvider.BasicPluginDllFile);
                if (pluginDll.ExistsFile)
                {
                    myQueue.Enqueue(() =>
                    {
                        myLogger.Info($"Remove {pluginDll}. Rider package should be used instead.");
                        pluginDll.DeleteFile();
                        FileSystemPath.Parse(pluginDll.FullPath + ".meta").DeleteFile();

                        // jetbrainsDir is usually "Assets\Plugins\Editor\JetBrains", however custom locations were also possible
                        var jetbrainsDir = installationInfoToRemove.PluginDirectory;
                        if (jetbrainsDir.GetChildren().Any() || jetbrainsDir.Name != "JetBrains") return;
                        jetbrainsDir.DeleteDirectoryNonRecursive();
                        FileSystemPath.Parse(jetbrainsDir.FullPath + ".meta").DeleteFile();
                        var pluginsEditorDir = jetbrainsDir.Directory;
                        if (pluginsEditorDir.GetChildren().Any() || pluginsEditorDir.Name != "Editor") return;
                        pluginsEditorDir.DeleteDirectoryNonRecursive();
                        FileSystemPath.Parse(pluginsEditorDir.FullPath + ".meta").DeleteFile();
                        var pluginsDir = pluginsEditorDir.Directory;
                        if (pluginsDir.GetChildren().Any() || pluginsDir.Name != "Plugins") return;
                        pluginsDir.DeleteDirectoryNonRecursive();
                        FileSystemPath.Parse(pluginsDir.FullPath+ ".meta").DeleteFile();
                    });
                }
                return;
            }

            // forcing fresh install due to being unable to provide proper setting until InputField is patched in Rider
            // ReSharper disable once ArgumentsStyleNamedExpression
            var installationInfo = myDetector.GetInstallationInfo(myCurrentVersion, previousInstallationDir: FileSystemPath.Empty);
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
                        userTitle = "Unity Editor plugin installed";
                        userMessage = $@"Please switch to Unity Editor to load the plugin.
                            Rider plugin v{myCurrentVersion} can be found at:
                            {installedPath.MakeRelativeTo(mySolution.SolutionDirectory)}.";
                        break;

                    case UnityPluginDetector.InstallReason.Update:
                        userTitle = "Unity Editor plugin updated";
                        userMessage = $@"Please switch to the Unity Editor to reload the plugin.
                            Rider plugin v{myCurrentVersion} can be found at:
                            {installedPath.MakeRelativeTo(mySolution.SolutionDirectory)}.";
                        break;

                    case UnityPluginDetector.InstallReason.ForceUpdateForDebug:
                        userTitle = "Unity Editor plugin updated (debug build)";
                        userMessage = $@"Please switch to the Unity Editor to reload the plugin.
                            Rider plugin v{myCurrentVersion} can be found at:
                            {installedPath.MakeRelativeTo(mySolution.SolutionDirectory)}.";
                        break;

                    case UnityPluginDetector.InstallReason.UpToDate:
                        userTitle = "Unity Editor plugin updated (up to date)";
                        userMessage = $@"Please switch to the Unity Editor to reload the plugin.
                            Rider plugin v{myCurrentVersion} can be found at:
                            {installedPath.MakeRelativeTo(mySolution.SolutionDirectory)}.";
                        break;

                    default:
                        myLogger.Error("Unexpected install reason: {0}", installationInfo.InstallReason);
                        return;
                }

                myLogger.Info(userTitle);

                var notification = new NotificationModel(userTitle, userMessage, true, RdNotificationEntryType.INFO);

                myShellLocks.ExecuteOrQueueEx(myLifetime, "UnityPluginInstaller.Notify", () => myNotifications.Notification(notification));
            }
        }

        public bool TryCopyFiles([NotNull] UnityPluginDetector.InstallationInfo installation, out FileSystemPath installedPath)
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

        private bool DoCopyFiles([NotNull] UnityPluginDetector.InstallationInfo installation, out FileSystemPath installedPath)
        {
            installedPath = null;

            var originPaths = new List<FileSystemPath>();
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

        private void RestoreFromBackup(Dictionary<FileSystemPath, FileSystemPath> backups)
        {
            foreach (var backup in backups)
            {
                myLogger.Info($"Restoring from backup {backup.Value} -> {backup.Key}");
                backup.Value.MoveFile(backup.Key, true);
            }
        }
    }
}