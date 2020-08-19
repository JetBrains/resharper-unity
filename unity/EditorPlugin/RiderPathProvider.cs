using System;
using System.IO;
using System.Linq;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor
{
  public class RiderPathProvider
  {
    private readonly IPluginSettings myPluginSettings;

    public RiderPathProvider(IPluginSettings pluginSettings)
    {
      myPluginSettings = pluginSettings;
    }

    /// <summary>
    /// If external editor is Rider and exists, it would be returned
    /// Otherwise, first of allFoundPaths would be returned
    /// </summary>
    /// <param name="externalEditor"></param>
    /// <returns>May return null, if nothing found.</returns>
    public string ValidateAndReturnActualRider(string externalEditor)
    {
      if (!string.IsNullOrEmpty(externalEditor))
      {
        var alreadySetPath = new FileInfo(externalEditor).FullName;
        if (RiderPathExist(alreadySetPath, myPluginSettings.OperatingSystemFamilyRider))
        {
          return alreadySetPath;
        }
      }
      
      var paths = RiderPathLocator.GetAllFoundPaths(myPluginSettings.OperatingSystemFamilyRider);
      return paths.FirstOrDefault();
    }
    
    /// <summary>
    /// If external editor is Rider, exists and is contained in the list of allFoundPaths, it would be returned
    /// Otherwise, first of allFoundPaths would be returned
    /// </summary>
    /// <param name="externalEditor"></param>
    /// <param name="allFoundPaths"></param>
    /// <returns>May return null, if nothing found.</returns>
    public string GetActualRider(string externalEditor, string[] allFoundPaths)
    {
      if (UnityUtils.UseRiderTestPath)
        return "riderTestPath";
      
      if (!string.IsNullOrEmpty(externalEditor))
      {
        var alreadySetPath = new FileInfo(externalEditor).FullName;
        if (RiderPathExist(alreadySetPath, myPluginSettings.OperatingSystemFamilyRider))
        {
          if (!allFoundPaths.Any() || allFoundPaths.Any() && allFoundPaths.Contains(alreadySetPath))
          {
            return alreadySetPath;
          }
        }
      }

      return allFoundPaths.FirstOrDefault();
    }

    internal static bool RiderPathExist(string path, OperatingSystemFamilyRider operatingSystemFamilyRider)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      // windows or mac
      var fileInfo = new FileInfo(path);
      if (!fileInfo.Name.ToLower().Contains("rider"))
        return false;
      var directoryInfo = new DirectoryInfo(path);
      var isMac = operatingSystemFamilyRider == OperatingSystemFamilyRider.MacOSX;
      return fileInfo.Exists || (isMac && directoryInfo.Exists);
    }
  }
}