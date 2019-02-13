using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using Microsoft.Win32;
#if UNITY_4_7 || UNITY_5_5
// ReSharper disable once RedundantUsingDirective
using Newtonsoft.Json;
#endif
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class RiderPathLocator
  {
    private static readonly ILog ourLogger = Log.GetLog<RiderPathLocator>();
    private readonly IPluginSettings myPluginSettings;

    public RiderPathLocator(IPluginSettings pluginSettings)
    {
      myPluginSettings = pluginSettings;
    }

    /// <summary>
    /// Returns RiderPath, if it exists
    /// </summary>
    /// <param name="externalEditor"></param>
    /// <param name="allFoundPaths"></param>
    /// <returns>May return null, if nothing found.</returns>
    public string GetDefaultRiderApp(string externalEditor, string[] allFoundPaths)
    {
      // update previously selected editor, if better one is found
      if (!string.IsNullOrEmpty(externalEditor))
      {
        var alreadySetPath = new FileInfo(externalEditor).FullName;
        if (RiderPathExist(alreadySetPath))
        {
          if (!allFoundPaths.Any() || allFoundPaths.Any() && allFoundPaths.Contains(alreadySetPath))
          {
            myPluginSettings.RiderPath = alreadySetPath;
            return alreadySetPath;
          }
        }
      }

      if (!string.IsNullOrEmpty(myPluginSettings.RiderPath) &&
          allFoundPaths.Contains(new FileInfo(myPluginSettings.RiderPath).FullName))
      {
        // Settings.RiderPath is good enough
      }
      else
        myPluginSettings.RiderPath = allFoundPaths.FirstOrDefault();

      return myPluginSettings.RiderPath;
    }

    private bool RiderPathExist(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      // windows or mac
      var fileInfo = new FileInfo(path);
      if (!fileInfo.Name.ToLower().Contains("rider"))
        return false;
      var directoryInfo = new DirectoryInfo(path);
      var isMac = myPluginSettings.OperatingSystemFamilyRider == OperatingSystemFamilyRider.MacOSX;
      return fileInfo.Exists || (isMac && directoryInfo.Exists);
    }

    internal static RiderInfo[] GetAllFoundInfos(OperatingSystemFamilyRider operatingSystemFamily)
    {
      try
      {
        switch (operatingSystemFamily)
        {
          case OperatingSystemFamilyRider.Windows:
          {
            return CollectRiderInfosWindows();
          }
          case OperatingSystemFamilyRider.MacOSX:
          {
            return CollectRiderInfosMac();
          }
          case OperatingSystemFamilyRider.Linux:
          {
            return CollectAllRiderPathsLinux();
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogException(e);
      }

      return new RiderInfo[0];
    }

    internal static string[] GetAllFoundPaths(OperatingSystemFamilyRider operatingSystemFamily)
    {
      return GetAllFoundInfos(operatingSystemFamily).Select(a=>a.Path).ToArray();
    }

    private static RiderInfo[] CollectAllRiderPathsLinux()
    {
      var home = Environment.GetEnvironmentVariable("HOME");
      if (string.IsNullOrEmpty(home))
        return new RiderInfo[0];
      var pathToBuildTxt = "../../build.txt";
      //$Home/.local/share/JetBrains/Toolbox/apps/Rider/ch-0/173.3994.1125/bin/rider.sh
      //$Home/.local/share/JetBrains/Toolbox/apps/Rider/ch-0/.channel.settings.json
      var toolboxRiderRootPath = Path.Combine(home, @".local/share/JetBrains/Toolbox/apps/Rider");
      var paths = CollectPathsFromToolbox(toolboxRiderRootPath, "bin", "rider.sh", false)
        .Select(a=>new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true)).ToList();


      // /home/ivan/.local/share/applications/jetbrains-rider.desktop
      var shortcut = new FileInfo(Path.Combine(home, @".local/share/applications/jetbrains-rider.desktop"));

      if (shortcut.Exists)
      {
        var lines = File.ReadAllLines(shortcut.FullName);
        foreach (var line in lines)
        {
          if (!line.StartsWith("Exec=\""))
            continue;
          var path = line.Split('"').Where((item, index)=>index==1).SingleOrDefault();
          if (string.IsNullOrEmpty(path))
            continue;
          var buildTxtPath = Path.Combine(path, pathToBuildTxt);
          var buildNumber = GetBuildNumber(buildTxtPath);
          if (paths.Any(a => a.Path == path)) // avoid adding similar build as from toolbox
            continue;
          paths.Add(new RiderInfo(buildNumber, path, false));
        }
      }

      return paths.ToArray();
    }

    private static RiderInfo[] CollectRiderInfosMac()
    {
      var pathToBuildTxt = "Contents/Resources/build.txt";

      // "/Applications/*Rider*.app"
      var folder = new DirectoryInfo("/Applications");
      var results = folder.GetDirectories("*Rider*.app")
        .Select(a=> new RiderInfo(GetBuildNumber(Path.Combine(a.FullName, pathToBuildTxt)), a.FullName, false))
        .ToList();

      // /Users/user/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/181.3870.267/Rider EAP.app
      var home = Environment.GetEnvironmentVariable("HOME");
      if (!string.IsNullOrEmpty(home))
      {
        var toolboxRiderRootPath = Path.Combine(home, @"Library/Application Support/JetBrains/Toolbox/apps/Rider");
        var paths = CollectPathsFromToolbox(toolboxRiderRootPath, "", "Rider*.app", true)
          .Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true));
        results.AddRange(paths);
      }

      return results.ToArray();
    }

    private static string GetBuildNumber(string path)
    {
      var file = new FileInfo(path);
      if (file.Exists)
        return File.ReadAllText(file.FullName);
      return string.Empty;
    }

    private static RiderInfo[] CollectRiderInfosWindows()
    {
      var pathToBuildTxt = "../../build.txt";

      var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
      var toolboxRiderRootPath = Path.Combine(localAppData, @"JetBrains\Toolbox\apps\Rider");
      var installPathsToolbox = CollectPathsFromToolbox(toolboxRiderRootPath, "bin", "rider64.exe", false).ToList();
      var installInfosToolbox = installPathsToolbox.Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true)).ToList();

      var installPaths = new List<string>();
      const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
      CollectPathsFromRegistry(registryKey, installPaths);
      const string wowRegistryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
      CollectPathsFromRegistry(wowRegistryKey, installPaths);

      var installInfos = installPaths.Select(a => new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, false)).ToList();
      installInfos.AddRange(installInfosToolbox);

      return installInfos.ToArray();
    }

    private static void CollectPathsFromRegistry(string registryKey, List<string> installPaths)
    {
      using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
      {
        if (key == null) return;
        foreach (var subkeyName in key.GetSubKeyNames().Where(a => a.Contains("Rider")))
        {
          using (var subkey = key.OpenSubKey(subkeyName))
          {
            var folderObject = subkey?.GetValue("InstallLocation");
            if (folderObject == null) continue;
            var folder = folderObject.ToString();
            var possiblePath = Path.Combine(folder, @"bin\rider64.exe");
            if (File.Exists(possiblePath))
              installPaths.Add(possiblePath);
          }
        }
      }
    }

#if !(UNITY_4_7 || UNITY_5_5)
    [UsedImplicitly]
    public static RiderInfo[] GetAllRiderPaths()
    {
      try
      {
        switch (SystemInfo.operatingSystemFamily)
        {
          case OperatingSystemFamily.Windows:
          {
            return CollectRiderInfosWindows();
          }
          case OperatingSystemFamily.MacOSX:
          {
            return CollectRiderInfosMac();
          }
          case OperatingSystemFamily.Linux:
          {
            return CollectAllRiderPathsLinux();
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogException(e);
      }

      return new RiderInfo[0];
    }
#endif

    private static string[] CollectPathsFromToolbox(string toolboxRiderRootPath, string dirName, string searchPattern, bool isMac)
    {
      if (!Directory.Exists(toolboxRiderRootPath))
        return new string[0];

      var channelDirs = Directory.GetDirectories(toolboxRiderRootPath);
      var paths = channelDirs.SelectMany(channelDir =>
        {
          try
          {            
            // use history.json - last entry stands for the active build https://jetbrains.slack.com/archives/C07KNP99D/p1547807024066500?thread_ts=1547731708.057700&cid=C07KNP99D
            var historyFile = Path.Combine(channelDir, ".history.json");
            if (File.Exists(historyFile))
            {
              var json = File.ReadAllText(historyFile);
              var build = ToolboxHistory.GetLatestBuildFromJson(json);
              if (build != null)
              {
                var buildDir = Path.Combine(channelDir, build);
                var executablePaths = GetExecutablePaths(dirName, searchPattern, isMac, buildDir);
                if (executablePaths.Any())
                  return executablePaths;
              }
            }
            
            var channelFile = Path.Combine(channelDir, ".channel.settings.json");
            if (File.Exists(channelFile))
            {
              var json = File.ReadAllText(channelFile).Replace("active-application", "active_application");
              var build = ToolboxInstallData.GetLatestBuildFromJson(json);
              if (build != null)
              {
                var buildDir = Path.Combine(channelDir, build);
                var executablePaths = GetExecutablePaths(dirName, searchPattern, isMac, buildDir);
                if (executablePaths.Any())
                  return executablePaths;
              }
            }
            
            // changes in toolbox json files format may brake the logic above, so return all found Rider installations
            return Directory.GetDirectories(channelDir)
              .SelectMany(buildDir=> GetExecutablePaths(dirName, searchPattern, isMac, buildDir));
          }
          catch (Exception e)
          {
            // do not write to Debug.Log, just log it.
            ourLogger.Warn(e, $"Failed to get RiderPath from {channelDir}");
          }

          return new string[0];
        })
        .Where(c => !string.IsNullOrEmpty(c))
        .ToArray();
      return paths;
    }

    private static string[] GetExecutablePaths(string dirName, string searchPattern, bool isMac, string buildDir)
    {
      var folder = Path.Combine(buildDir, dirName);
      if (!isMac)
        return new[] {Path.Combine(folder, searchPattern)}.Where(File.Exists).ToArray();
      return new DirectoryInfo(folder).GetDirectories(searchPattern).Select(f => f.FullName)
        .Where(Directory.Exists).ToArray();
    }

    // Disable the "field is never assigned" compiler warning. We never assign it, but Unity does.
    // Note that Unity disable this warning in the generated C# projects
#pragma warning disable 0649

    [Serializable]
    class ToolboxHistory
    {
      public List<ItemNode> history;

      [CanBeNull]
      public static string GetLatestBuildFromJson(string json)
      {
        try
        {
#if UNITY_4_7 || UNITY_5_5
          return JsonConvert.DeserializeObject<ToolboxHistory>(json).history.LastOrDefault()?.item.build;
#else
          return JsonUtility.FromJson<ToolboxHistory>(json).history.LastOrDefault()?.item.build;
#endif
        }
        catch (Exception)
        {
          ourLogger.Warn($"Failed to get latest build from json {json}");
        }
        return null;
      }
    }

    [Serializable]
    class ItemNode
    {
      public BuildNode item;
    }
    
    [Serializable]
    class BuildNode
    {
      public string build;
    }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    [Serializable]
    class ToolboxInstallData
    {
      // ReSharper disable once InconsistentNaming
      public ActiveApplication active_application;

      [CanBeNull]
      public static string GetLatestBuildFromJson(string json)
      {
        try
        {
#if UNITY_4_7 || UNITY_5_5
          var toolbox = JsonConvert.DeserializeObject<ToolboxInstallData>(json);
#else
          var toolbox = JsonUtility.FromJson<ToolboxInstallData>(json);
#endif
          var builds = toolbox.active_application.builds;
          if (builds != null && builds.Any())
            return builds.First();
        }
        catch (Exception)
        {
          ourLogger.Warn($"Failed to get latest build from json {json}");
        }
        return null;
      }
    }

    [Serializable]
    class ActiveApplication
    {
      // ReSharper disable once InconsistentNaming
      public List<string> builds;
    }

#pragma warning restore 0649

    public struct RiderInfo
    {
      public string Presentation;
      public string BuildVersion;
      public string Path;

      public RiderInfo(string buildVersion, string path, bool isToolbox)
      {
        BuildVersion = buildVersion;
        Path = new FileInfo(path).FullName; // normalize separators

        var version = string.Empty;
        if (buildVersion.Length > 3)
          version = buildVersion.Substring(3);

        var presentation = "Rider " + version;
        if (isToolbox)
          presentation += " (JetBrains Toolbox)";

        Presentation = presentation;
      }
    }
  }
}