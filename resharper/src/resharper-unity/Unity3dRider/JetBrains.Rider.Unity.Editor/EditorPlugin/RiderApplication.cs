using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace JetBrains.Rider.Unity.Editor
{
  public static class RiderApplication
  {
    /// <summary>
    /// Returns RiderPath, if it exists
    /// </summary>
    /// <param name="externalEditor"></param>
    /// <param name="allFoundPaths"></param>
    /// <returns>May return null, if nothing found.</returns>
    public static string GetDefaultRiderApp(string externalEditor, string[] allFoundPaths)
    {
      // update previously selected editor, if better one is found
      if (!string.IsNullOrEmpty(externalEditor))
      {
        var alreadySetPath = new FileInfo(externalEditor).FullName;
        if (RiderPathExist(alreadySetPath))
        {
          if (!allFoundPaths.Any() || allFoundPaths.Any() && allFoundPaths.Contains(alreadySetPath))
          {
            Settings.RiderPath = alreadySetPath;
            return alreadySetPath;
          }
        }
      }
      
      if (!string.IsNullOrEmpty(Settings.RiderPath) && allFoundPaths.Contains(new FileInfo(Settings.RiderPath).FullName))
      {
        // Settings.RiderPath is good enough
      }
      else
        Settings.RiderPath = allFoundPaths.FirstOrDefault();

      return Settings.RiderPath;
    }
  
    private static bool RiderPathExist(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      // windows or mac
      var fileInfo = new FileInfo(path);
      if (!fileInfo.Name.ToLower().Contains("rider"))
        return false;
      var directoryInfo = new DirectoryInfo(path);
      return fileInfo.Exists || (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.MacOSX &&
                                 directoryInfo.Exists);
    }

    internal static string[] GetAllFoundPaths()
    {
      // fix separators
      return GetAllRiderPaths().Select(a => new FileInfo(a).FullName).ToArray();
    }

    private static string[] GetAllRiderPaths()
    {
      switch (SystemInfoRiderPlugin.operatingSystemFamily)
      {
        case OperatingSystemFamilyRider.Windows:
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
            var newPaths = newPathLnks
              .Select(newPathLnk => new FileInfo(ShortcutResolver.Resolve(newPathLnk.FullName)))
              .Where(fi => File.Exists(fi.FullName))
              .ToArray()
              .OrderByDescending(fi => FileVersionInfo.GetVersionInfo(fi.FullName).ProductVersion)
              .Select(a => a.FullName).ToArray();

            return newPaths;
          }

          break;

        case OperatingSystemFamilyRider.MacOSX:
          // "/Applications/*Rider*.app"
          //"~/Applications/JetBrains Toolbox/*Rider*.app"
          string[] foldersMac =
          {
            "/Applications", Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Applications/JetBrains Toolbox")
          };
          var newPathsMac = foldersMac.Select(b => new DirectoryInfo(b)).Where(a => a.Exists)
            .SelectMany(c => c.GetDirectories("*Rider*.app"))
            .Select(a => a.FullName).ToArray();
          return newPathsMac;
      }

      return new string[0];
    }
  }
}