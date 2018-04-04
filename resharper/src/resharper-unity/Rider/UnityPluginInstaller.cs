using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application.Environment;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginInstaller : UnityReferencesTracker.IHandler, UnresolvedUnityReferencesTracker.IHandler
    {
        private readonly JetHashSet<FileSystemPath> myPluginInstallations;
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly IShellLocks myShellLocks;
        private readonly UnityPluginDetector myDetector;
        private readonly ILogger myLogger;
        private readonly RdNotificationsModel myNotifications;
        private readonly PluginPathsProvider myPluginPathsProvider;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        private readonly ProcessingQueue myQueue;

        public UnityPluginInstaller(
            Lifetime lifetime,
            ILogger logger,
            ISolution solution,
            IShellLocks shellLocks,
            UnityPluginDetector detector,
            RdNotificationsModel notifications,
            ISettingsStore settingsStore,
            PluginPathsProvider pluginPathsProvider)
        {
            myPluginInstallations = new JetHashSet<FileSystemPath>();

            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myShellLocks = shellLocks;
            myDetector = detector;
            myNotifications = notifications;
            myPluginPathsProvider = pluginPathsProvider;

            myBoundSettingsStore = settingsStore.BindToContextLive(myLifetime, ContextRange.Smart(solution.ToDataContext()));
            myQueue = new ProcessingQueue(myShellLocks, myLifetime);
        }

        void UnityReferencesTracker.IHandler.OnSolutionLoaded(UnityProjectsCollection solution)
        {
            myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.OnSolutionLoaded", () => InstallPluginIfRequired(solution.UnityProjectLifetimes.Keys));

            BindToInstallationSettingChange();
        }

        void UnityReferencesTracker.IHandler.OnReferenceAdded(IProject unityProject, Lifetime projectLifetime)
        {
            myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.ResolvedReferenceAdded", () => InstallPluginIfRequired(new[] {unityProject}));
        }

        void UnresolvedUnityReferencesTracker.IHandler.OnReferenceAdded(IProject unityProject)
        {
            myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.UnresolvedReferenceAdded", () => InstallPluginIfRequired(new[] {unityProject}));
        }

        private void BindToInstallationSettingChange()
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnitySettings s) => s.InstallUnity3DRiderPlugin);
            myBoundSettingsStore.GetValueProperty<bool>(myLifetime, entry, null).Change.Advise(myLifetime, CheckAllProjectsIfAutoInstallEnabled);
        }

        private void CheckAllProjectsIfAutoInstallEnabled(PropertyChangedEventArgs<bool> args)
        {
            if (!args.GetNewOrNull())
                return;
            
            myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.CheckAllProjectsIfAutoInstallEnabled", () => InstallPluginIfRequired(mySolution.GetAllProjects().Where(p => p.IsUnityProject()).ToList()));
        }

        private void InstallPluginIfRequired(ICollection<IProject> projects)
        {
            if (projects.Count == 0)
                return;
            
            if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                return;
            
            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.InstallUnity3DRiderPlugin))
                return;

            // forcing fresh install due to being unable to provide proper setting until InputField is patched in Rider
            // ReSharper disable once ArgumentsStyleNamedExpression
            var installationInfo = myDetector.GetInstallationInfo(previousInstallationDir: FileSystemPath.Empty);
            if (!installationInfo.ShouldInstallPlugin)
            {
                myLogger.Info("Plugin should not be installed.");
                if (installationInfo.ExistingFiles.Count > 0)
                    myLogger.Info("Already existing plugin files:\n{0}", string.Join("\n", installationInfo.ExistingFiles));
                
                return;
            }
            
            myQueue.Enqueue(() =>
            {
                Install(installationInfo);
                myPluginInstallations.Add(mySolution.SolutionFilePath);
            });
        }

        readonly Version currentVersion = typeof(UnityPluginInstaller).Assembly.GetName().Version;

        private void Install(UnityPluginDetector.InstallationInfo installationInfo)
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

            if (currentVersion == installationInfo.Version)
            {
                myLogger.Verbose($"Plugin v{installationInfo.Version} already installed.");
                return;
            }

            var isFreshInstall = installationInfo.Version == UnityPluginDetector.ZeroVersion;
            if (isFreshInstall)
                myLogger.Info("Fresh install");

            FileSystemPath installedPath;

            if (!TryCopyFiles(installationInfo, out installedPath))
            {
                myLogger.Warn("Plugin was not installed");
            }
            else
            {
                string userTitle;
                string userMessage;

                if (isFreshInstall)
                {
                    userTitle = "Unity: plugin installed";
                    userMessage =
                        $@"Rider plugin v{
                                currentVersion
                            } for the Unity Editor was automatically installed for the project '{mySolution.Name}'
This allows better integration between the Unity Editor and Rider IDE.
The plugin file can be found on the following path:
{installedPath.MakeRelativeTo(mySolution.SolutionFilePath)}.
Please switch back to Unity to make plugin file appear in the solution.";
                }
                else
                {
                    userTitle = "Unity: plugin updated";
                    userMessage = $"Rider plugin was succesfully upgraded to version {currentVersion}";
                }

                myLogger.Info(userTitle);

                var notification = new RdNotificationEntry(userTitle,
                    userMessage, true,
                    RdNotificationEntryType.INFO);
                
                myShellLocks.ExecuteOrQueueEx(myLifetime, "UnityPluginInstaller.Notify", () => myNotifications.Notification.Fire(notification));
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
                var editorPluginPath = editorPluginPathDir.Combine(UnityPluginDetector.BasicPluginDllFile);

                var targetPath = installation.PluginDirectory.Combine(editorPluginPath.Name);
                try
                {
                    editorPluginPath.CopyFile(targetPath, true);
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

                installedPath = installation.PluginDirectory.Combine(UnityPluginDetector.BasicPluginDllFile);
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