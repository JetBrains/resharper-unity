using System.IO;
using System.Linq;
using JetBrains.Rider.PathLocator;

namespace JetBrains.Rider.Unity.Editor
{
  internal class RiderPathProvider
  {
    public static readonly RiderPathLocator RiderPathLocator;

    static RiderPathProvider()
    {
      RiderPathLocator = new RiderPathLocator(new RiderLocatorEnvironment());
    }

    private readonly RiderPathLocator myRiderPathLocator;

    public RiderPathProvider() : this(RiderPathLocator)
    {
    }

    // Allows tests to inject a locator backed by a non-Unity environment, since the production
    // RiderLocatorEnvironment.CurrentOS calls UnityEngine.SystemInfo, unavailable outside the editor.
    internal RiderPathProvider(RiderPathLocator riderPathLocator)
    {
      myRiderPathLocator = riderPathLocator;
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
        if (RiderPathExist(alreadySetPath, myRiderPathLocator.RiderLocatorEnvironment.CurrentOS))
        {
          return alreadySetPath;
        }
      }

      var paths = myRiderPathLocator.GetAllRiderPaths();
      return paths.Select(a=>a.Path).FirstOrDefault();
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
        if (RiderPathExist(alreadySetPath, myRiderPathLocator.RiderLocatorEnvironment.CurrentOS))
        {
          if (!allFoundPaths.Any() || allFoundPaths.Any() && allFoundPaths.Contains(alreadySetPath))
          {
            return alreadySetPath;
          }
        }
      }

      return allFoundPaths.FirstOrDefault();
    }

    internal static bool RiderPathExist(string path, OS osRider)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      // windows or mac
      var fileInfo = new FileInfo(path);
      if (!fileInfo.Name.ToLower().Contains("rider"))
        return false;
      var directoryInfo = new DirectoryInfo(path);
      var isMac = osRider == OS.MacOSX;
      return fileInfo.Exists || (isMac && directoryInfo.Exists);
    }
  }
}