using System.Collections.Generic;
using System.Linq;
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
    public static readonly InstallationInfo ShouldNotInstall = new InstallationInfo(false, FileSystemPath.Empty, EmptyArray<FileSystemPath>.Instance, new Version2());

    public UnityPluginDetector(ILogger logger)
    {
    }

    public InstallationInfo GetInstallationInfo(IProject project)
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
        .GetChildFiles()
        .Select(f => f.Name)
        .Where(name => UnityPluginInstaller.PluginFiles.Contains(name))  // case sensitivity?
        .Select(name => jetBrainsDir.Combine(name))
        .ToArray();

      return ExistingInstallation(jetBrainsDir, existingFiles);
    }

    [CanBeNull]
    private static FileSystemPath GetAssetsDirectory([NotNull] IProject project)
    {
      return project.ProjectFileLocation?.Directory.GetChildDirectories("Assets").SingleItem();
    }

    private static InstallationInfo NotInstalled(FileSystemPath pluginDir)
    {
      return new InstallationInfo(true, pluginDir, EmptyArray<FileSystemPath>.Instance, new Version2());
    }

    private static InstallationInfo ExistingInstallation(FileSystemPath pluginDir, FileSystemPath[] installedFiles)
    {
      var installedVersion = new Version2(); // TODO: detect version by comment in file
      
      return new InstallationInfo(true, pluginDir, installedFiles, installedVersion);
    }

    public class InstallationInfo
    {
      public readonly bool ShouldInstallPlugin;

      [NotNull]
      public readonly FileSystemPath PluginDirectory;

      [NotNull]
      public readonly FileSystemPath[] InstalledFiles;

      [NotNull]
      public readonly Version2 Version;

      public InstallationInfo(bool shouldInstallPlugin, [NotNull] FileSystemPath pluginDirectory,
        [NotNull] FileSystemPath[] installedFiles, [NotNull] Version2 version)
      {
        ShouldInstallPlugin = shouldInstallPlugin;
        PluginDirectory = pluginDirectory;
        InstalledFiles = installedFiles;
        Version = version;
      }
    }
  }
}