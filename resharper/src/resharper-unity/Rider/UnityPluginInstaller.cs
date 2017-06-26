#if RIDER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.Rider.Model.Notifications;
using JetBrains.Threading;
using JetBrains.Util;

using JetBrains.Application.Threading;

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
    private readonly RdNotificationsModel myNotifications;
    private readonly IContextBoundSettingsStoreLive myBoundSettingsStore;

    private static readonly string ourResourceNamespace =
      typeof(KnownTypes).Namespace + ".Unity3dRider.Assets.Plugins.Editor.JetBrains.";

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

      myShellLocks.ReentrancyGuard.ExecuteOrQueueEx("UnityPluginInstaller.CheckAllProjects", () => myShellLocks.ExecuteWithReadLock(CheckAllProjects));
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

        FileSystemPath installedPath;
        
        if (!TryInstall(installationInfo, out installedPath))
        {
          myLogger.LogMessage(LoggingLevel.WARN, "Plugin was not installed");
        }
        else
        {
          string userTitle;
          string userMessage;

          if (isFreshInstall)
          {
            userTitle = "Unity: plugin installed";
            userMessage = 
              $@"Rider plugin v{currentVersion} for the Unity Editor was automatically installed for the project '{mySolution.Name}'
This allows better integration between the Unity Editor and Rider IDE.
The plugin file can be found on the following path:
{installedPath.MakeRelativeTo(mySolution.SolutionFilePath)}";
            
          }
          else
          {
            userTitle = "Unity: plugin updated";
            userMessage = $"Rider plugin was succesfully upgraded from version {installationInfo.Version} to {currentVersion}";
          }
          
          myLogger.LogMessage(LoggingLevel.INFO, userTitle);
          
          var notification = new RdNotificationEntry(userTitle,
            userMessage, true,
            RdNotificationEntryType.INFO);
          myNotifications.Notification.Fire(notification);
        }

        myPluginInstallations.Add(project.ProjectFileLocation);
      }
    }

    public bool TryInstall([NotNull] UnityPluginDetector.InstallationInfo installation, out FileSystemPath installedPath)
    {
      installedPath = null;
      try
      {
        installation.PluginDirectory.CreateDirectory();

        return DoInstall(installation, out installedPath);
      }
      catch (Exception e)
      {
        myLogger.LogException(LoggingLevel.ERROR, e, ExceptionOrigin.OuterWorld, "Plugin installation failed");
        return false;
      }
    }

    private bool DoInstall([NotNull] UnityPluginDetector.InstallationInfo installation, out FileSystemPath installedPath)
    {
      installedPath = null;
      
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
        myLogger.LogMessage(LoggingLevel.INFO, $"Restoring from backup {backup.Value} -> {backup.Key}");
        backup.Value.MoveFile(backup.Key, true);
      }
    }
  }
}
#endif