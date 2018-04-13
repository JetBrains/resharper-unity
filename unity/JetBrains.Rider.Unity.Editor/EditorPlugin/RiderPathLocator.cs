using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Util.Logging;
using Microsoft.Win32;
#if UNITY_4_7 || UNITY_5_5 || UNITY_2017_3
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
        .Select(a=>new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true)).ToArray();
      if (paths.Any())
        return paths;
      return Directory.GetDirectories(toolboxRiderRootPath)
        .SelectMany(Directory.GetDirectories)
        .Select(b => Path.Combine(b, "bin/rider.sh"))
        .Where(File.Exists)
        .Select(a=>new RiderInfo(GetBuildNumber(Path.Combine(a, pathToBuildTxt)), a, true))
        .ToArray();
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
      if (File.Exists(path))
        return File.ReadAllText(path);
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
      return new RiderInfo[0];
    }
#endif 

    private static string[] CollectPathsFromToolbox(string toolboxRiderRootPath, string dirName, string searchPattern, bool isMac)
    {
      if (!Directory.Exists(toolboxRiderRootPath))
        return new string[0];

      var channelFiles = Directory.GetDirectories(toolboxRiderRootPath)
        .Select(b => Path.Combine(b, ".channel.settings.json")).Where(File.Exists).ToArray();

      var paths = channelFiles.SelectMany(a =>
        {
          try
          {
            var channelDir = Path.GetDirectoryName(a);
            var json = File.ReadAllText(a).Replace("active-application", "active_application");
            var toolbox = ToolboxInstallData.FromJson(json);
            var builds = toolbox.active_application.builds;
            if (builds.Any())
            {
              var build = builds.First();
              var folder = Path.Combine(Path.Combine(channelDir, build), dirName);
              if (!isMac)
                return new[] {Path.Combine(folder, searchPattern)};
              return new DirectoryInfo(folder).GetDirectories(searchPattern).Select(f => f.FullName);
            }
          }
          catch (Exception e)
          {
            ourLogger.Warn(e, "Failed to get RiderPath via .channel.settings.json");
          }

          return new string[0];
        })
        .Where(c => !string.IsNullOrEmpty(c))
        .ToArray();
      return paths;
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    [Serializable]
    class ToolboxInstallData
    {
      public ActiveApplication active_application;

      public static ToolboxInstallData FromJson(string json)
      {
#if UNITY_4_7 || UNITY_5_5
        return JsonConvert.DeserializeObject<ToolboxInstallData>(json);
#else
        return JsonUtility.FromJson<ToolboxInstallData>(json);
#endif
      }
    }

    [Serializable]
    class ActiveApplication
    {
      public List<string> builds;
    }

    public struct RiderInfo
    {
      public string Presentation;
      public string BuildVersion;
      public string Path;

      public RiderInfo(string buildVersion, string path, bool isToolbox)
      {
        BuildVersion = buildVersion;
        Path = new FileInfo(path).FullName; // normalize separators

        var presentation = "Rider";
        if (buildVersion.Length >= 3)
          presentation += " " + buildVersion.Substring(3);
        if (isToolbox)
          presentation += " (JetBrains Toolbox)";
        else
        {
          if (path.Length>=18)
            presentation += $" ({path.Substring(0, 15)}...)";
          else
            presentation += $" ({path})";
        }
              
        // hack around https://fogbugz.unity3d.com/default.asp?940857_tirhinhe3144t4vn
        Presentation = presentation.Replace("/", ":");
      }
    }
  }
}