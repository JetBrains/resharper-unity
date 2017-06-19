using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application.Settings;

#if RIDER
using JetBrains.Application.Threading;
#endif

#if WAVE08
using JetBrains.Application;
#endif

using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Unity3dRider
{
#if RIDER
  [SolutionComponent]
#endif
  public class UnityPluginInstaller
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
      typeof(UnityPluginInstaller).Namespace + ".Assets.Plugins.Editor.JetBrains.";

    private readonly object mySyncObj = new object();

    public UnityPluginInstaller(
      Lifetime lifetime,
      ILogger logger,
      ISolution solution,
      IShellLocks shellLocks,
      UnityPluginDetector detector,
      RdNotificationsModel notifications,
      ISettingsStore settingsStore,
      ProjectReferenceChangeTracker changeTracker)
    {
      myPluginInstallations = new JetHashSet<FileSystemPath>();

      myLifetime = lifetime;
      myLogger = logger;
      mySolution = solution;
      myShellLocks = shellLocks;
      myDetector = detector;
      myNotifications = notifications;
      myBoundSettingsStore = settingsStore.BindToContextLive(myLifetime, ContextRange.Smart(solution.ToDataContext()));
      
      BindToInstallationSettingChange();
      
      changeTracker.RegisterProjectChangeHandler(InstallPluginIfRequired);
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

      myShellLocks.ReentrancyGuard.ExecuteOrQueue("UnityPluginInstaller.CheckAllProjects", () => myShellLocks.ExecuteWithReadLock(CheckAllProjects));
    }

    private void CheckAllProjects()
    {
      foreach (var project in mySolution.GetAllProjects())
      {
        InstallPluginIfRequired(myLifetime, project);
      }
    }

    private void InstallPluginIfRequired(Lifetime lifetime, [NotNull] IProject project)
    {
      if (!myBoundSettingsStore.GetValue((UnityPluginSettings s) => s.InstallUnity3DRiderPlugin))
        return;
      
      if (myPluginInstallations.Contains(project.ProjectFileLocation))
        return;
      
      var installationInfo = myDetector.GetInstallationInfo(project);
      if (!installationInfo.ShouldInstallPlugin)
        return;

      var currentVersion = typeof(UnityPluginInstaller).Assembly.GetName().Version;
      if (currentVersion <= installationInfo.Version)
        return;
      
      var isFreshInstall = installationInfo.Version == new Version();
      if (isFreshInstall)
        myLogger.LogMessage(LoggingLevel.INFO, "Fresh install");

      lock (mySyncObj)
      {
        if (myPluginInstallations.Contains(project.ProjectFileLocation))
          return;

        if (!TryInstall(installationInfo))
        {
          myLogger.LogMessage(LoggingLevel.WARN, "Plugin was not installed");
        }
        else
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

          myLogger.LogMessage(LoggingLevel.INFO, logMessage);
          myNotifications.Notification.Fire(notification);
        }

        myPluginInstallations.Add(project.ProjectFileLocation);
      }
    }

    public bool TryInstall([NotNull] UnityPluginDetector.InstallationInfo installation)
    {
      try
      {
        installation.PluginDirectory.CreateDirectory();

        return DoInstall(installation);
      }
      catch (Exception e)
      {
        myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.OuterWorld, "Plugin installation failed");
        return false;
      }
    }

    private bool DoInstall([NotNull] UnityPluginDetector.InstallationInfo installation)
    {
      var backups = installation.InstalledFiles.ToDictionary(f => f, f => f.AddSuffix(".backup"));
      
      foreach (var originPath in installation.InstalledFiles)
      {
        var backupPath = backups[originPath];
        originPath.MoveFile(backupPath, true);
        myLogger.LogMessage(LoggingLevel.INFO, $"backing up: {originPath.Name} -> {backupPath.Name}");
      }

      try
      {
        var path = installation.PluginDirectory.Combine(UnityPluginDetector.MergedPluginFile);

        var resourceName = ourResourceNamespace + UnityPluginDetector.MergedPluginFile;
        using (var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
          if (resourceStream == null)
          {
            myLogger.LogMessage(LoggingLevel.ERROR, "Plugin file not found in manifest resources. " + resourceName);
            
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
        myLogger.LogMessage(LoggingLevel.INFO, $"Restoring from backup {backup.Value} -> {backup.Key}");
        backup.Value.MoveFile(backup.Key, true);
      }
    }
  }
}