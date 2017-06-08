using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Unity3dRider
{
#if RIDER
  [SolutionComponent]
#endif
  public class UnityPluginDetector
  {
    private readonly ILogger myLogger;
    private static readonly string[] ourPluginFilesV180 = {"RiderAssetPostprocessor.cs", "RiderPlugin.cs"};
    
    public static readonly string MergedPluginFile = "Unity3DRider.cs";
    
    private static readonly Regex ourVersionRegex = new Regex(@"// ((?:[0-9]\.)+[0-9])", RegexOptions.Compiled);
    
    public static readonly InstallationInfo ShouldNotInstall = new InstallationInfo(false, FileSystemPath.Empty, EmptyArray<FileSystemPath>.Instance, new Version());

    public UnityPluginDetector(ILogger logger)
    {
      myLogger = logger;
    }

    public InstallationInfo GetInstallationInfo(IProject project)
    {
      try
      {
        if (!project.IsUnityProject())
          return ShouldNotInstall;

        var assetsDir = GetAssetsDirectory(project);
        if (assetsDir == null)
          return ShouldNotInstall;

        var jetBrainsDir = assetsDir
          .CombineWithShortName("Plugins")
          .CombineWithShortName("Editor")
          .CombineWithShortName("JetBrains");

        if (!jetBrainsDir.ExistsDirectory)
          return NotInstalled(jetBrainsDir);

        var existingFiles = jetBrainsDir
          .GetChildFiles("*.cs")
          .ToArray();

        if (existingFiles.Length == 0)
          return NotInstalled(jetBrainsDir);

        return ExistingInstallation(jetBrainsDir, existingFiles);
      }
      catch (Exception e)
      {
        myLogger.LogExceptionSilently(e);
        return ShouldNotInstall;
      }
    }

    [CanBeNull]
    private static FileSystemPath GetAssetsDirectory([NotNull] IProject project)
    {
      return project.ProjectFileLocation?.Directory.GetChildDirectories("Assets").SingleItem();
    }

    private static InstallationInfo NotInstalled(FileSystemPath pluginDir)
    {
      return new InstallationInfo(true, pluginDir, EmptyArray<FileSystemPath>.Instance, new Version());
    }

    private static InstallationInfo ExistingInstallation(FileSystemPath pluginDir, FileSystemPath[] installedFiles)
    {
      Version installedVersion;
      if (installedFiles.Select(f => f.Name).IsEquivalentTo(ourPluginFilesV180))
        installedVersion = new Version(1, 8, 0, 0);
      else if (installedFiles.Length == 1 && installedFiles[0].Name == MergedPluginFile)
        installedVersion = GetVersionFromFile(installedFiles[0]);
      else
        installedVersion = new Version();
      
      return new InstallationInfo(installedVersion.Major > 0, pluginDir, installedFiles, installedVersion);
    }

    private static Version GetVersionFromFile(FileSystemPath mergedFile)
    {
      string firstLine;
      using (var fs = mergedFile.OpenStream(FileMode.Open, FileAccess.Read))
        using (var sr = new StreamReader(fs))
        {
          firstLine = sr.ReadLine();
        }
      if (firstLine == null)
        return new Version();

      var match = ourVersionRegex.Match(firstLine);
      if (!match.Success)
        return new Version();
      
      Version version;
      return Version.TryParse(match.Groups[1].Value, out version) ? version : new Version();
    }

    public class InstallationInfo
    {
      public readonly bool ShouldInstallPlugin;

      [NotNull]
      public readonly FileSystemPath PluginDirectory;

      [NotNull]
      public readonly FileSystemPath[] InstalledFiles;

      [NotNull]
      public readonly Version Version;

      public InstallationInfo(bool shouldInstallPlugin, [NotNull] FileSystemPath pluginDirectory,
        [NotNull] FileSystemPath[] installedFiles, [NotNull] Version version)
      {
        ShouldInstallPlugin = shouldInstallPlugin;
        PluginDirectory = pluginDirectory;
        InstalledFiles = installedFiles;
        Version = version;
      }
    }
  }
}