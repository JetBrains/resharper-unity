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
    /// Returns RiderPath, if it exists
    /// </summary>
    /// <param name="externalEditor"></param>
    /// <param name="allFoundPaths"></param>
    /// <returns>May return null, if nothing found.</returns>
    public string GetActualRider(string externalEditor, string[] allFoundPaths)
    {
      if (PluginEntryPoint.ourTestModeEnabled)
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