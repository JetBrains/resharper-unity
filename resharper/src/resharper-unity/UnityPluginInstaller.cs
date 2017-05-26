using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application.Notifications;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityPluginInstaller
    {
        private readonly ILogger myLogger;
        private readonly RdNotificationsModel myNotifications;

        private static readonly string resourceNamespace = typeof(UnityPluginInstaller).Namespace + ".Unity3dRider.Assets.Plugins.Editor.JetBrains.";
        private static readonly string[] ourPluginFiles = {"RiderAssetPostprocessor.cs", "RiderPlugin.cs"};
        
        private readonly object mySyncObj = new object();

        public UnityPluginInstaller(
            ProjectReferenceChangeTracker changeTracker,
            ILogger logger,
            RdNotificationsModel notifications)
        {
            myLogger = logger;
            myNotifications = notifications;
#if RIDER
            changeTracker.RegisterProjectChangeHandler(InstallPluginIfRequired);
#endif
        }

        private void InstallPluginIfRequired(Lifetime lifetime, [NotNull] IProject project)
        {
            if (!IsPluginNeeded(project))
                return;

            myLogger.LogMessage(LoggingLevel.INFO, "Unity -> Rider plugin missing, installing");

            if (TryInstall(project))
            {
                myLogger.LogMessage(LoggingLevel.INFO, "Plugin installed");
                
                var notification = new RdNotificationEntry("Plugin installed", "Unity -> Rider plugin was added to Unity project", true, RdNotificationEntryType.INFO);
                myNotifications.Notification.Fire(notification);
            }
            else
            {
                myLogger.LogMessage(LoggingLevel.WARN, "Plugin was not installed");
            }
        }
        
        public bool IsPluginNeeded([NotNull] IProject project)
        {
            var assetsDir = GetAssetsDirectory(project);
            if (assetsDir == null)
                return false; // not a Unity project

            var jetBrainsDir = assetsDir
                .CombineWithShortName("Plugins")
                .CombineWithShortName("Editor")
                .CombineWithShortName("JetBrains");

            if (!jetBrainsDir.ExistsDirectory)
                return true;

            var existingFiles = new JetHashSet<string>(jetBrainsDir.GetChildFiles().Select(f => f.Name));
            return ourPluginFiles.Any(f => !existingFiles.Contains(f));
        }

        public bool TryInstall([NotNull] IProject project)
        {
            try
            {
                var assetsDir = GetAssetsDirectory(project);
                if (assetsDir == null)
                {
                    myLogger.LogMessage(LoggingLevel.ERROR, "Assets directory not found when trying to install plugin");
                    return false;
                }

                var pluginDir = assetsDir
                    .CombineWithShortName("Plugins")
                    .CombineWithShortName("Editor")
                    .CombineWithShortName("JetBrains");

                pluginDir.CreateDirectory();

                lock (mySyncObj)
                {
                    return CopyFiles(pluginDir);
                }
            }
            catch (Exception e)
            {
                myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.OuterWorld, "Plugin installation failed");
                return false;
            }
        }

        private bool CopyFiles(FileSystemPath pluginDir)
        {
            var updatedFileCount = 0;
            var existingPluginFiles = pluginDir.GetChildFiles();
            foreach (var filename in ourPluginFiles)
            {
                var path = pluginDir.Combine(filename);
                if (existingPluginFiles.Contains(path))
                    continue;

                var resourceName = resourceNamespace + filename;
                using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        myLogger.LogMessage(LoggingLevel.ERROR, "Plugin file not found in manifest resources. " + filename);
                        return false;
                    }
                        
                    using (var fileStream = path.OpenStream(FileMode.CreateNew))
                    {
                        resourceStream.CopyTo(fileStream);
                        updatedFileCount++;
                    }
                }
            }

            return updatedFileCount > 0;
        }

        [CanBeNull]
        private static FileSystemPath GetAssetsDirectory([NotNull] IProject project)
        {
            return project.ProjectFileLocation?.Directory.GetChildDirectories("Assets").SingleItem();
        }
    }
}