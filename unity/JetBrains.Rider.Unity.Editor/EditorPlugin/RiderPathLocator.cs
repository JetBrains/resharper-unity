using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Util.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    internal static string[] GetAllFoundPaths(OperatingSystemFamilyRider operatingSystemFamily)
    {
      // fix separators
      return GetAllRiderPaths(operatingSystemFamily).Select(a => new FileInfo(a).FullName).ToArray();
    }

    private static string[] GetAllRiderPaths(OperatingSystemFamilyRider operatingSystemFamily)
    {
      switch (operatingSystemFamily)
      {
        case OperatingSystemFamilyRider.Windows:
        {
          string[] folders =
          {
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\JetBrains", Path.Combine(
              Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
              @"Microsoft\Windows\Start Menu\Programs\JetBrains Toolbox")
          };

          var newPathLnks = folders.Select(b => new DirectoryInfo(b)).Where(a => a.Exists)
            .SelectMany(c => c.GetFiles("*Rider*.lnk")).ToArray();
          if (newPathLnks.Any())
          {
            var results = newPathLnks
              .Select(newPathLnk => new FileInfo(ShortcutResolver.Resolve(newPathLnk.FullName)))
              .Where(fi => File.Exists(fi.FullName))
              .ToArray()
              .OrderByDescending(fi => FileVersionInfo.GetVersionInfo(fi.FullName).ProductVersion)
              .Select(a => a.FullName).ToArray();

            return results;
          }
        }
          break;

        case OperatingSystemFamilyRider.MacOSX:
        {
          // /Users/user/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/181.3870.267/Rider EAP.app
          var home = Environment.GetEnvironmentVariable("HOME");
          if (string.IsNullOrEmpty(home))
            return new string[0];

          var toolboxRiderRootPath = Path.Combine(home, @"Library/Application Support/JetBrains/Toolbox/apps/Rider");
          var paths = GetAllRiderPaths(toolboxRiderRootPath, "", "Rider*.app", true);
          // "/Applications/*Rider*.app"
          //"~/Applications/JetBrains Toolbox/*Rider*.app"
          string[] folders =
          {
            "/Applications",
            Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Applications/JetBrains Toolbox")
          };
          var results = folders.Select(b => new DirectoryInfo(b)).Where(a => a.Exists)
            .SelectMany(c => c.GetDirectories("*Rider*.app"))
            .Select(a => a.FullName).ToList();
          results.AddRange(paths);
          return results.ToArray();
        }

        case OperatingSystemFamilyRider.Linux:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (string.IsNullOrEmpty(home))
            return new string[0];
          //$Home/.local/share/JetBrains/Toolbox/apps/Rider/ch-0/173.3994.1125/bin/rider.sh
          //$Home/.local/share/JetBrains/Toolbox/apps/Rider/ch-0/.channel.settings.json
          var toolboxRiderRootPath = Path.Combine(home, @".local/share/JetBrains/Toolbox/apps/Rider");
          var paths = GetAllRiderPaths(toolboxRiderRootPath, "bin", "rider.sh", false);
          if (paths.Any())
            return paths;
          return Directory.GetDirectories(toolboxRiderRootPath).SelectMany(Directory.GetDirectories)
              .Select(b => Path.Combine(b, "bin/rider.sh")).Where(File.Exists).ToArray();
        }
      }

      return new string[0];
    }

    private static string[] GetAllRiderPaths(string toolboxRiderRootPath, string dirName, string searchPattern, bool isMac)
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
            var json = File.ReadAllText(a);
            var data = (JObject) JsonConvert.DeserializeObject(json);
            var builds = data["active-application"]["builds"];
            if (builds.HasValues)
            {
              var build = builds.First;
              var folder = Path.Combine(Path.Combine(channelDir, build.Value<string>()), dirName);
              if (!isMac)
                return new[] {Path.Combine(folder, searchPattern)};
              return new DirectoryInfo(folder).GetDirectories(searchPattern).Select(f=>f.FullName);
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
  }
}