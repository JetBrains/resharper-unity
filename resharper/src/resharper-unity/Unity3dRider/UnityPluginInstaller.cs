using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Unity3dRider
{
#if RIDER
  [SolutionComponent]
#endif
  public class UnityPluginInstaller
  {
    public static readonly string[] PluginFiles = {"RiderAssetPostprocessor.cs", "RiderPlugin.cs"};
    
    private readonly JetHashSet<FileSystemPath> myPluginInstallations;
    private readonly UnityPluginDetector myDetector;
    private readonly ILogger myLogger;
    private readonly RdNotificationsModel myNotifications;

    private static readonly string ourResourceNamespace =
      typeof(UnityPluginInstaller).Namespace + ".Assets.Plugins.Editor.JetBrains.";

    private readonly object mySyncObj = new object();

    public UnityPluginInstaller(
      UnityPluginDetector detector,
      ProjectReferenceChangeTracker changeTracker,
      RdNotificationsModel notifications,
      ILogger logger)
    {
      myPluginInstallations = new JetHashSet<FileSystemPath>();
      myDetector = detector;
      myLogger = logger;
      myNotifications = notifications;
      changeTracker.RegisterProjectChangeHandler(InstallPluginIfRequired);
    }

    private void InstallPluginIfRequired(Lifetime lifetime, [NotNull] IProject project)
    {
      if (myPluginInstallations.Contains(project.ProjectFileLocation))
        return;
      
      var installationInfo = myDetector.GetInstallationInfo(project);
      if (!installationInfo.ShouldInstallPlugin)
        return;

      var currentVersion = new Version2(typeof(UnityPluginInstaller).Assembly.GetName().Version);
      if (currentVersion <= installationInfo.Version)
        return;
      
      var isFreshInstall = installationInfo.InstalledFiles.Length == 0;
      if (isFreshInstall)
        myLogger.LogMessage(LoggingLevel.INFO, "Fresh install");

      lock (mySyncObj)
      {
        if (myPluginInstallations.Contains(project.ProjectFileLocation))
          return;
        
        if (TryInstall(installationInfo))
        {
          string logMessage;
          RdNotificationEntry notification;

          if (isFreshInstall)
          {
            logMessage = "Plugin installed";
            notification = new RdNotificationEntry("Plugin installed",
              $"Unity -> Rider plugin v{currentVersion} was added to Unity project", true,
              RdNotificationEntryType.INFO);
          }
          else
          {
            logMessage = "Plugin updated";
            notification = new RdNotificationEntry("Plugin updated",
              $"Unity -> Rider plugin was updated in the Unity project.\n{installationInfo.Version} -> {currentVersion}",
              true, RdNotificationEntryType.INFO);
          }

          myPluginInstallations.Add(project.ProjectFileLocation);
          myLogger.LogMessage(LoggingLevel.INFO, logMessage);
          myNotifications.Notification.Fire(notification);
        }
        else
        {
          myLogger.LogMessage(LoggingLevel.WARN, "Plugin was not installed");
        }
      }
    }

    public bool TryInstall([NotNull] UnityPluginDetector.InstallationInfo installation)
    {
      try
      {
        installation.PluginDirectory.CreateDirectory();

        return CopyFiles(installation.PluginDirectory);
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
      foreach (var filename in PluginFiles)
      {
        var path = pluginDir.Combine(filename);
        var resourceName = ourResourceNamespace + filename;
        using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
          if (resourceStream == null)
          {
            myLogger.LogMessage(LoggingLevel.ERROR, "Plugin file not found in manifest resources. " + filename);
            return false;
          }

          using (var fileStream = path.OpenStream(FileMode.OpenOrCreate))
          {
            resourceStream.CopyTo(fileStream);
            updatedFileCount++;
          }
        }
      }

      return updatedFileCount > 0;
    }
  }
}