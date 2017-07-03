#if RIDER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Plugins.Unity.Utils;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginInstaller : IProjectChangeHandler
    {
        private readonly JetHashSet<FileSystemPath> myPluginInstallations;
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly IShellLocks myShellLocks;
        private readonly UnityPluginDetector myDetector;
        private readonly ILogger myLogger;
        private readonly RdNotificationsModel myNotifications;
        private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

        private static readonly string ourResourceNamespace =
            typeof(KnownTypes).Namespace + ".Unity3dRider.Assets.Plugins.Editor.JetBrains.";

        private readonly ProcessingQueue myQueue;

        public UnityPluginInstaller(
            Lifetime lifetime,
            ILogger logger,
            ISolution solution,
            IShellLocks shellLocks,
            UnityPluginDetector detector,
            RdNotificationsModel notifications,
            ISettingsStore settingsStore)
        {
            myPluginInstallations = new JetHashSet<FileSystemPath>();

            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myShellLocks = shellLocks;
            myDetector = detector;
            myNotifications = notifications;
            
            myBoundSettingsStore = settingsStore.BindToContextLive(myLifetime, ContextRange.Smart(solution.ToDataContext()));
            myQueue = new ProcessingQueue(myShellLocks, myLifetime);
        }
        
        public void OnSolutionLoaded(UnityProjectsCollection solution)
        {
            myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.OnSolutionLoaded", () => InstallPluginIfRequired(solution.UnityProjectLifetimes.Keys));

            BindToInstallationSettingChange();
        }

        public void OnProjectChanged(IProject unityProject, Lifetime projectLifetime)
        {
            myShellLocks.ExecuteOrQueueReadLockEx(myLifetime, "UnityPluginInstaller.OnProjectChanged", () => InstallPluginIfRequired(new[] {unityProject}));
        }

        private void BindToInstallationSettingChange()
        {
            var entry = myBoundSettingsStore.Schema.GetScalarEntry((UnityPluginSettings s) => s.InstallUnity3DRiderPlugin);
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
            if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                return;
            
            if (!myBoundSettingsStore.GetValue((UnityPluginSettings s) => s.InstallUnity3DRiderPlugin))
                return;

            if (projects.Count == 0)
                return;
            
            // forcing fresh install due to being unable to provide proper setting until InputField is patched in Rider
            // ReSharper disable once ArgumentsStyleNamedExpression
            var installationInfo = myDetector.GetInstallationInfo(projects, previousInstallationDir: FileSystemPath.Empty);
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

            var currentVersion = typeof(UnityPluginInstaller).Assembly.GetName().Version;
            if (currentVersion <= installationInfo.Version)
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
{installedPath.MakeRelativeTo(mySolution.SolutionFilePath)}";
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

            var backups = installation.ExistingFiles.ToDictionary(f => f, f => f.AddSuffix(".backup"));

            foreach (var originPath in installation.ExistingFiles)
            {
                var backupPath = backups[originPath];
                originPath.MoveFile(backupPath, true);
                myLogger.Info($"backing up: {originPath.Name} -> {backupPath.Name}");
            }

            try
            {
                var path = installation.PluginDirectory.Combine(UnityPluginDetector.MergedPluginFile);

                var resourceName = ourResourceNamespace + UnityPluginDetector.MergedPluginFile;
                using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        myLogger.Error("Plugin file not found in manifest resources. " + resourceName);

                        RestoreFromBackup(backups);

                        return false;
                    }

                    using (var fileStream = path.OpenStream(FileMode.OpenOrCreate))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }

                foreach (var backup in backups)
                {
                    backup.Value.DeleteFile();
                }

                installedPath = path;

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
#endif