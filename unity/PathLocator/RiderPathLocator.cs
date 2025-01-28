using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace JetBrains.Rider.PathLocator
{
  /// <summary>
  /// This code is a modified version of the JetBrains resharper-unity plugin listed under Apache License 2.0 license:
  /// https://github.com/JetBrains/resharper-unity/blob/master/unity/JetBrains.Rider.Unity.Editor/EditorPlugin/RiderPathLocator.cs
  /// </summary>
  public class RiderPathLocator
  {
    [PublicAPI]
    public readonly IRiderLocatorEnvironment RiderLocatorEnvironment;

    public RiderPathLocator(IRiderLocatorEnvironment riderLocatorEnvironment)
    {
      RiderLocatorEnvironment = riderLocatorEnvironment;
    }
    
    [UsedImplicitly] // Used in com.unity.ide.rider
    public RiderInfo[] GetAllRiderPaths()
    {
      var results = new List<RiderInfo>();
      try
      {
        var toolboxPath = GetToolboxPath();
        var jsonFile = Path.Combine(toolboxPath, "state.json");
        if (File.Exists(jsonFile))
          results.AddRange(ToolboxState.GetStateFromJson(this, File.ReadAllText(jsonFile)));
        
        switch (RiderLocatorEnvironment.CurrentOS)
        {
          case OS.Windows:
            results.AddRange(CollectRiderInfosWindows());
            break;
          case OS.MacOSX:
            results.AddRange(CollectRiderInfosMac());
            break;
          case OS.Linux:
            results.AddRange(CollectAllRiderPathsLinux());
            break;
          default:
            throw new ArgumentOutOfRangeException();
        }
      }
      catch (Exception e)
      {
        RiderLocatorEnvironment.Error("GetAllRiderPaths failed", e);
      }

      return results.Distinct().ToArray();
    }

    private RiderInfo[] CollectAllRiderPathsLinux()
    {
      var installInfos = new List<RiderInfo>();
      var appsPath = GetAppsInstallLocation();

      installInfos.AddRange(CollectToolbox20Linux(appsPath, "*rider*", "bin/rider"));
      installInfos.AddRange(CollectToolbox20Linux(appsPath, "*fleet*", "bin/Fleet"));

      var riderRootPath = Path.Combine(appsPath, "Rider");
      installInfos.AddRange(CollectPathsFromToolbox(riderRootPath, "bin", "rider.sh", false)
        .Select(a => new RiderInfo(this, a, true)).ToList());

      var fleetRootPath = Path.Combine(appsPath, "Fleet");
      installInfos.AddRange(CollectPathsFromToolbox(fleetRootPath, "bin", "Fleet", false)
        .Select(a => new RiderInfo(this, a, true)).ToList());

      var home = Environment.GetEnvironmentVariable("HOME");
      if (!string.IsNullOrEmpty(home))
      {
        //$Home/.local/share/applications/jetbrains-rider.desktop
        var shortcut = new FileInfo(Path.Combine(home, @".local/share/applications/jetbrains-rider.desktop"));

        if (shortcut.Exists)
        {
          var lines = File.ReadAllLines(shortcut.FullName);
          foreach (var line in lines)
          {
            if (!line.StartsWith("Exec=\""))
              continue;
            var path = line.Split('"').Where((_, index) => index == 1).SingleOrDefault();
            if (string.IsNullOrEmpty(path))
              continue;
            if (!File.Exists(path))
              continue;
            
            installInfos.Add(new RiderInfo(this, path, false));
          }
        }
      }

      // snap install
      var snapInstallPath = "/snap/rider/current/bin/rider.sh";
      if (new FileInfo(snapInstallPath).Exists)
        installInfos.Add(new RiderInfo(this, snapInstallPath, false));

      return installInfos.ToArray();
    }

    private IEnumerable<RiderInfo> CollectToolbox20Linux(string appsPath, string pattern, string relPath)
    {
      var result = new List<RiderInfo>();
      if (string.IsNullOrEmpty(appsPath) || !Directory.Exists(appsPath))
        return result;

      CollectToolbox20(appsPath, pattern, relPath, result);

      return result;
    }

    private RiderInfo[] CollectRiderInfosMac()
    {
      var installInfos = new List<RiderInfo>();

      installInfos.AddRange(CollectFromApplications("*Rider*.app"));
      installInfos.AddRange(CollectFromApplications("*Fleet*.app"));

      var appsPath = GetAppsInstallLocation();
      var riderRootPath = Path.Combine(appsPath, "Rider");
      installInfos.AddRange(CollectPathsFromToolbox(riderRootPath, "", "Rider*.app", true)
        .Select(a => new RiderInfo(this, a, true)));

      var fleetRootPath = Path.Combine(appsPath, "Fleet");
      installInfos.AddRange(CollectPathsFromToolbox(fleetRootPath, "", "Fleet*.app", true)
        .Select(a => new RiderInfo(this, a, true)));

      return installInfos.ToArray();
    }

    private RiderInfo[] CollectFromApplications(string productMask)
    {
      var result = new List<RiderInfo>();
      var folder = new DirectoryInfo("/Applications");
      if (folder.Exists)
      {
        result.AddRange(folder.GetDirectories(productMask)
          .Select(a => new RiderInfo(this, a.FullName, false))
          .ToList());
      }

      var home = Environment.GetEnvironmentVariable("HOME");
      if (!string.IsNullOrEmpty(home))
      {
        var userFolder = new DirectoryInfo(Path.Combine(home, "Applications"));
        if (userFolder.Exists)
        {
          result.AddRange(userFolder.GetDirectories(productMask)
            .Select(a => new RiderInfo(this, a.FullName, false))
            .ToList());
        }
      }

      return result.ToArray();
    }

    private RiderInfo[] CollectRiderInfosWindows()
    {
      var installInfos = new List<RiderInfo>();

      var appsPath = GetAppsInstallLocation();
      var riderRootPath = Path.Combine(appsPath, "Rider");
      installInfos.AddRange(CollectPathsFromToolbox(riderRootPath, "bin", "rider64.exe", false).ToList()
        .Select(a => new RiderInfo(this, a, true)).ToList());

      var fleetRootPath = Path.Combine(appsPath, "Fleet");
      installInfos.AddRange(CollectPathsFromToolbox(fleetRootPath, string.Empty, "Fleet.exe", false).ToList()
        .Select(a => new RiderInfo(this, a, true)).ToList());
      
      const string registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
      CollectPathsFromRegistry(registryKey, installInfos);
      const string wowRegistryKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
      CollectPathsFromRegistry(wowRegistryKey, installInfos);

      return installInfos.ToArray();
    }

    private void CollectToolbox20(string dir, string pattern, string relPath, List<RiderInfo> result)
    {
      var directoryInfo = new DirectoryInfo(dir);
      if (!directoryInfo.Exists)
        return;

      foreach (var riderDirectory in directoryInfo.GetDirectories(pattern))
      {
        var executable = Path.Combine(riderDirectory.FullName, relPath);

        if (File.Exists(executable))
        {
          result.Add(new RiderInfo(this, executable, false)); // false, because we can't check if it is Toolbox or not anyway
        }
      }
    }

    private string GetAppsInstallLocation()
    {
      var toolboxPath = GetToolboxPath();
      var settingsJson = Path.Combine(toolboxPath, ".settings.json");

      if (File.Exists(settingsJson))
      {
        var path = SettingsJson.GetInstallLocationFromJson(RiderLocatorEnvironment, File.ReadAllText(settingsJson));
        if (!string.IsNullOrEmpty(path))
          return path;
      }
      
      return Path.Combine(toolboxPath, "apps");
    }

    private string GetToolboxPath()
    {
      string localAppData = string.Empty;
      switch (RiderLocatorEnvironment.CurrentOS)
      {
        case OS.Windows:
        {
          localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
          break;
        }

        case OS.MacOSX:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (!string.IsNullOrEmpty(home))
          {
            localAppData = Path.Combine(home, @"Library/Application Support");
          }
          break;
        }

        case OS.Linux:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (!string.IsNullOrEmpty(home))
          {
            localAppData = Path.Combine(home, @".local/share");
          }
          break;
        }
        default:
          throw new Exception("Unknown OS");
      }

      var toolboxPath = Path.Combine(localAppData, @"JetBrains/Toolbox");
      return toolboxPath;
    }

    [PublicAPI]
    public ProductInfo GetBuildVersion(string path)
    {
      var buildTxtFileInfo = new FileInfo(Path.Combine(path, GetRelativePathToBuildTxt()));
      var dir = buildTxtFileInfo.DirectoryName;
      if (!Directory.Exists(dir))
        return null;
      var buildVersionFile = new FileInfo(Path.Combine(dir, "product-info.json"));
      if (!buildVersionFile.Exists)
        return null;
      var json = File.ReadAllText(buildVersionFile.FullName);
      return ProductInfo.GetProductInfo(RiderLocatorEnvironment, json);
    }

    [PublicAPI]
    public Version GetBuildNumber(string riderPath)
    {
      Version buildNum = null;
      try
      {
        buildNum = GetBuildNumberWithBuildTxt(riderPath);
      }
      catch (Exception e)
      {
        RiderLocatorEnvironment.Warn($"Failed to get buildNum from {riderPath}", e);
      }

      return buildNum ?? GetBuildNumberFromInput(riderPath);
    }

    private Version GetBuildNumberWithBuildTxt(string riderPath)
    {
      var buildTxtFileInfo = new FileInfo(Path.Combine(riderPath, GetRelativePathToBuildTxt()));
      if (!buildTxtFileInfo.Exists)
        return null;
      var text = File.ReadAllText(buildTxtFileInfo.FullName);
      var index = text.IndexOf("-", StringComparison.Ordinal) + 1; // RD-191.7141.355
      if (index <= 0)
        return null;

      var versionText = text.Substring(index);
      return GetBuildNumberFromInput(versionText);
    }

    [CanBeNull]
    private Version GetBuildNumberFromInput(string input)
    {
      if (string.IsNullOrEmpty(input))
        return null;

      var match = Regex.Match(input, @"(?<major>\d+)\.(?<minor>\d+)(\.(?<build>\d+))?");
      var groups = match.Groups;
      Version version = null;
      if (match.Success)
      {
        var major = match.Groups["major"].Value;
        var minor = match.Groups["minor"].Value;
        version = match.Groups["build"].Success
          ? new Version($"{major}.{minor}.{match.Groups["build"].Value}")
          : new Version($"{major}.{minor}");
      }

      return version;
    }

    [UsedImplicitly] // Rider package
    public bool GetIsToolbox(string path)
    {
      return Path.GetFullPath(path).StartsWith(Path.GetFullPath(GetAppsInstallLocation()));
    }

    private string GetRelativePathToBuildTxt()
    {
      switch (RiderLocatorEnvironment.CurrentOS)
      {
        case OS.Windows: 
        case OS.Linux:
          return "../../build.txt";
        case OS.MacOSX:
          return "Contents/Resources/build.txt";
      }

      throw new Exception("Unknown OS");
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private void CollectPathsFromRegistry(string registryKey, List<RiderInfo> installPaths)
    {
      using (var key = Registry.CurrentUser.OpenSubKey(registryKey))
      {
        CollectPathsFromRegistry(installPaths, key);
      }

      using (var key = Registry.LocalMachine.OpenSubKey(registryKey))
      {
        CollectPathsFromRegistry(installPaths, key);
      }
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
    private void CollectPathsFromRegistry(List<RiderInfo> installPaths, RegistryKey key)
    {
      if (key == null) return;
      foreach (var subkeyName in key.GetSubKeyNames())
      {
        using (var subkey = key.OpenSubKey(subkeyName))
        {
          var folderObject = subkey?.GetValue("InstallLocation");
          if (folderObject == null) continue;
          var folder = folderObject.ToString();
          if (folder.Length == 0) continue;
          var displayName = subkey.GetValue("DisplayName");
          if (displayName == null) continue;
          if (displayName.ToString().Contains("Rider"))
          {
            try // possible "illegal characters in path"
            {
              var possiblePath = Path.Combine(folder, @"bin\rider64.exe");
              if (File.Exists(possiblePath))
                installPaths.Add(new RiderInfo(this, possiblePath, subkeyName.Contains("JetBrains Toolbox")));
            }
            catch (ArgumentException)
            {
            }
          }
          else if (displayName.ToString().Contains("Fleet"))
          {
            try // possible "illegal characters in path"
            {
              var possiblePath = Path.Combine(folder, @"Fleet.exe");
              if (File.Exists(possiblePath))
                installPaths.Add(new RiderInfo(this, possiblePath, subkeyName.Contains("JetBrains Toolbox")));
            }
            catch (ArgumentException)
            {
            }
          }
        }
      }
    }

    private string[] CollectPathsFromToolbox(string productRootPathInToolbox, string dirName,
      string searchPattern,
      bool isMac)
    {
      if (!Directory.Exists(productRootPathInToolbox))
        return new string[0];

      var channelDirs = Directory.GetDirectories(productRootPathInToolbox);
      var paths = channelDirs.SelectMany(channelDir =>
        {
          try
          {
            // use history.json - last entry stands for the active build https://jetbrains.slack.com/archives/C07KNP99D/p1547807024066500?thread_ts=1547731708.057700&cid=C07KNP99D
            var historyFile = Path.Combine(channelDir, ".history.json");
            if (File.Exists(historyFile))
            {
              var json = File.ReadAllText(historyFile);
              var build = ToolboxHistory.GetLatestBuildFromJson(RiderLocatorEnvironment, json);
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
              var build = ToolboxInstallData.GetLatestBuildFromJson(RiderLocatorEnvironment, json);
              if (build != null)
              {
                var buildDir = Path.Combine(channelDir, build);
                var executablePaths = GetExecutablePaths(dirName, searchPattern, isMac, buildDir);
                if (executablePaths.Any())
                  return executablePaths;
              }
            }

            // changes in toolbox json files format may brake the logic above, so return all found installations
            return Directory.GetDirectories(channelDir)
              .SelectMany(buildDir => GetExecutablePaths(dirName, searchPattern, isMac, buildDir));
          }
          catch (Exception e)
          {
            // do not write to Debug.Log, just log it.
            RiderLocatorEnvironment.Warn($"Failed to get path from {channelDir}", e);
          }

          return new string[0];
        })
        .Where(c => !string.IsNullOrEmpty(c))
        .ToArray();
      return paths;
    }

    private string[] GetExecutablePaths(string dirName, string searchPattern, bool isMac, string buildDir)
    {
      var folder = new DirectoryInfo(Path.Combine(buildDir, dirName));
      if (!folder.Exists)
        return new string[0];

      if (!isMac)
        return new[] { Path.Combine(folder.FullName, searchPattern) }.Where(File.Exists).ToArray();
      return folder.GetDirectories(searchPattern).Select(f => f.FullName)
        .Where(Directory.Exists).ToArray();
    }
    // Disable the "field is never assigned" compiler warning. We never assign it, but Unity does.
    // Note that Unity disable this warning in the generated C# projects
#pragma warning disable 0649
    
    [Serializable]
    class ToolboxState
    {
      [UsedImplicitly] public int version;
      [UsedImplicitly] public List<Tool> tools;

      [Annotations.NotNull]
      public static RiderInfo[] GetStateFromJson(RiderPathLocator riderPathLocator, string json)
      {
        try
        {
          var state = riderPathLocator.RiderLocatorEnvironment.FromJson<ToolboxState>(json);
          var version = state.version;
          if (version > 1) return new RiderInfo[0];
          
          var tools = state.tools;
          return tools.Where(tool => tool.toolId is "Rider" or "Fleet").Select(a => new RiderInfo(true, $"{a.displayName} {a.displayVersion}", riderPathLocator.GetBuildNumberFromInput(a.buildNumber),
            riderPathLocator.RiderLocatorEnvironment.CurrentOS != OS.MacOSX ? Path.Combine(a.installLocation, a.launchCommand) : a.installLocation)).ToArray();
        }
        catch (Exception)
        {
          riderPathLocator.RiderLocatorEnvironment.Warn($"Failed to get toolbox state from {json}");
        }

        return new RiderInfo[0];
      }
      
      [Serializable]
      public class Tool
      {
        [UsedImplicitly] public string toolId;
        [UsedImplicitly] public string displayName;
        [UsedImplicitly] public string displayVersion;
        [UsedImplicitly] public string buildNumber;
        [UsedImplicitly] public string installLocation;
        [UsedImplicitly] public string launchCommand;
      }
    }
    
    [Serializable]
    class SettingsJson
    {
      // ReSharper disable once InconsistentNaming
      [UsedImplicitly] public string install_location; // We never assign it, but Unity does.

      [CanBeNull]
      public static string GetInstallLocationFromJson(IRiderLocatorEnvironment riderLocatorEnvironment, string json)
      {
        try
        {
          return riderLocatorEnvironment.FromJson<SettingsJson>(json).install_location;
        }
        catch (Exception)
        {
          riderLocatorEnvironment.Warn($"Failed to get install_location from json {json}");
        }

        return null;
      }
    }

    [Serializable]
    class ToolboxHistory
    {
      [UsedImplicitly] public List<ItemNode> history;

      [CanBeNull]
      public static string GetLatestBuildFromJson(IRiderLocatorEnvironment riderLocatorEnvironment, string json)
      {
        try
        {
          return riderLocatorEnvironment.FromJson<ToolboxHistory>(json).history.LastOrDefault()?.item.build;
        }
        catch (Exception)
        {
          riderLocatorEnvironment.Warn($"Failed to get latest build from json {json}");
        }

        return null;
      }
    }

    [Serializable]
    class ItemNode
    {
      [UsedImplicitly] public BuildNode item;
    }

    [Serializable]
    class BuildNode
    {
      [UsedImplicitly] public string build;
    }

    [Serializable]
    public class ProductInfo
    {
      [UsedImplicitly] public string version;
      [UsedImplicitly] public string versionSuffix;

      [CanBeNull]
      internal static ProductInfo GetProductInfo(IRiderLocatorEnvironment riderLocatorEnvironment, string json)
      {
        try
        {
          return riderLocatorEnvironment.FromJson<ProductInfo>(json);
        }
        catch (Exception)
        {
          riderLocatorEnvironment.Warn($"Failed to get version from json {json}");
        }

        return null;
      }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    [Serializable]
    class ToolboxInstallData
    {
      // ReSharper disable once InconsistentNaming
      [UsedImplicitly] public ActiveApplication active_application;

      [CanBeNull]
      public static string GetLatestBuildFromJson(IRiderLocatorEnvironment riderLocatorEnvironment, string json)
      {
        try
        {
          var builds = riderLocatorEnvironment.FromJson<ToolboxInstallData>(json).active_application.builds;
          if (builds != null && builds.Any())
            return builds.First();
        }
        catch (Exception)
        {
          riderLocatorEnvironment.Warn($"Failed to get latest build from json {json}");
        }

        return null;
      }
    }

    [Serializable]
    class ActiveApplication
    {
      [UsedImplicitly] public List<string> builds;
    }

#pragma warning restore 0649
    
    [Serializable]
    public struct RiderInfo
    {
      public bool IsToolbox;
      public string Presentation;
      public string BuildNumber;
      public ProductInfo ProductInfo;
      public string Path;

      public RiderInfo(RiderPathLocator riderPathLocator, string path, bool isToolbox)
        : this(path,
          isToolbox, riderPathLocator.GetBuildNumber(path), riderPathLocator.GetBuildVersion(path))
      {
      }

      public RiderInfo(bool isToolbox, string presentation, Version buildNumber, string path)
      {
        IsToolbox = isToolbox;
        Presentation = presentation;
        BuildNumber = buildNumber != null ? buildNumber.ToString() : string.Empty;
        Path = new FileInfo(path).FullName; // normalize separators

        ProductInfo = null;
      }

      [PublicAPI]
      public RiderInfo(string path, bool isToolbox, Version buildNumber, ProductInfo productInfo)
      {
        BuildNumber =  buildNumber != null ? buildNumber.ToString() : string.Empty;
        ProductInfo = productInfo;
        
        var fileInfo = new FileInfo(path);
        var productName = GetProductNameForPresentation(fileInfo);
        Path = fileInfo.FullName; // normalize separators
        var presentation = $"{productName} {buildNumber}";

        if (productInfo != null && !string.IsNullOrEmpty(productInfo.version))
        {
          var suffix = string.IsNullOrEmpty(productInfo.versionSuffix) ? "" : $" {productInfo.versionSuffix}";
          presentation = $"{productName} {productInfo.version}{suffix}";
        }

        if (isToolbox)
          presentation += " (JetBrains Toolbox)";

        Presentation = presentation;
        IsToolbox = isToolbox;
      }

      public override bool Equals(object obj)
      {
        if (obj == null) return false;
        if (obj.GetType() != GetType()) return false;
        return Path == ((RiderInfo)obj).Path;
      }
      
      public override int GetHashCode()
      {
        return Path.GetHashCode();
      }

      private static string GetProductNameForPresentation(FileInfo path)
      {
        var filename = path.Name;
        if (filename.StartsWith("rider", StringComparison.OrdinalIgnoreCase))
          return "Rider";
        if (RiderFileOpener.IsFleet(path))
          return "Fleet";
        return filename;
      }
    }
  }
}