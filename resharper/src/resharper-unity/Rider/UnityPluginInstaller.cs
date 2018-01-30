using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.BuildScript.Application;
using JetBrains.Application.Environment;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;
using JetBrains.Application.Threading;
using JetBrains.Build.Serialization;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.Util.Special;

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
        private readonly ApplicationPackages myApplicationPackages;
        private readonly ApplicationPackagesLocallyInstalled myApplicationPackagesLocallyInstalled;
        private readonly IEnumerable<ApplicationPackageArtifact> myPackages;
        private readonly IDeployedPackagesExpandLocationResolver myResolver;
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
            ISettingsStore settingsStore,
            ApplicationPackages applicationPackages,
            ApplicationPackagesLocallyInstalled applicationPackagesLocallyInstalled,
            IEnumerable<ApplicationPackageArtifact> packages, IDeployedPackagesExpandLocationResolver resolver)
        {
            myPluginInstallations = new JetHashSet<FileSystemPath>();

            myLifetime = lifetime;
            myLogger = logger;
            mySolution = solution;
            myShellLocks = shellLocks;
            myDetector = detector;
            myNotifications = notifications;
            myApplicationPackages = applicationPackages;
            myApplicationPackagesLocallyInstalled = applicationPackagesLocallyInstalled;
            myPackages = packages;
            myResolver = resolver;

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
            
            InstallFromResource(@"Library\resharper-unity-libs\nunit3.5.0\nunit.framework.dll", ".Unity3dRider.Library.resharper_unity_libs.nunit3._5._0.nunit.framework.dll");
            
            if (myPluginInstallations.Contains(mySolution.SolutionFilePath))
                return;
            
            if (!myBoundSettingsStore.GetValue((UnitySettings s) => s.InstallUnity3DRiderPlugin))
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
        
        Version currentVersion = typeof(UnityPluginInstaller).Assembly.GetName().Version;

        private void InstallFromResource(string relPath, string namespacePath)
        {
            var solutionDir = mySolution.SolutionFilePath.Directory;
            if (!ProjectExtensions.IsSolutionGeneratedByUnity(solutionDir))
            {
                myLogger.Info($"No Assets directory in the same directory as solution. Skipping {relPath} installation.");
                return;
            }
            
            var fullPath = solutionDir.Combine(relPath);
            if (!fullPath.IsAbsolute)
            {
                myLogger.Info($"Path {fullPath} is not Absolute.");
                return;
            }
            if (fullPath.ExistsFile)
            {
                myLogger.Info($"Already exists {fullPath}");
                return;
            }
            
            myQueue.Enqueue(() =>
            {
                var assembly = Assembly.GetExecutingAssembly();
                //JetBrains.ReSharper.Plugins.Unity.Unity3dRider.Library.resharper_unity_libs.nunit3._5._0.nunit.framework.dll
                var resourceName = typeof(KnownTypes).Namespace +namespacePath;

                try
                {
                    using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        fullPath.Directory.CreateDirectory();
                        using (var fileStream = fullPath.OpenStream(FileMode.Create))
                        {
                            if (resourceStream == null)
                                myLogger.Error("Plugin file not found in manifest resources. " + resourceName);
                            else
                                resourceStream.CopyTo(fileStream);
                        }    
                    }
                }
                catch (Exception e)
                {
                    myLogger.LogExceptionSilently(e);
                    myLogger.Warn($"{relPath} was not restored from resourse.");
                }
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

                var package = myApplicationPackages.FindPackageWithAssembly(Assembly.GetExecutingAssembly(), OnError.LogException);
                var subplatformName = package.SubplatformName;
                //myApplicationPackagesLocallyInstalled.
                FileSystemPath dir = myResolver.GetDeployedPackageDirectory(package);

                //var path = ApplicationPackagesLocallyInstalled(package);
                var name = "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll";
                Tuple<FileSystemPath, string>[] installs =
                {
                    new Tuple<FileSystemPath, string>(installation.PluginDirectory.Combine(name), ourResourceNamespace + name),
                };               

                foreach (Tuple<FileSystemPath, string> install in installs)
                {
                    if (!InstallFromResourceWithBackup(install.Item2, backups, install.Item1)) return false;    
                }
                
                foreach (var backup in backups)
                {
                    backup.Value.DeleteFile();
                }

                installedPath = installation.PluginDirectory.Combine(UnityPluginDetector.MergedPluginFile);
                return true;
            }
            catch (Exception e)
            {
                myLogger.LogExceptionSilently(e);

                RestoreFromBackup(backups);

                return false;
            }
        }

        private bool InstallFromResourceWithBackup(string resourceName, Dictionary<FileSystemPath, FileSystemPath> backups, FileSystemPath path)
        {
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

            return true;
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