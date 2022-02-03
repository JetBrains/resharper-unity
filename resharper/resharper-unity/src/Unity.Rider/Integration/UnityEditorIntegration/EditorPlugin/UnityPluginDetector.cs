using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Util;
using JetBrains.Util.Logging;
using ProjectExtensions = JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.ProjectExtensions;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration.EditorPlugin
{
    [SolutionComponent]
    public class UnityPluginDetector
    {
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;

        private const string PluginCsFile = "Unity3DRider.cs";

        public UnityPluginDetector(ISolution solution, ILogger logger)
        {
            mySolution = solution;
            myLogger = logger;
        }

        [NotNull]
        public InstallationInfo GetInstallationInfo(Version newVersion, VirtualFileSystemPath previousInstallationDir = null)
        {
            myLogger.Verbose("GetInstallationInfo.");
            try
            {
                var assetsDir = mySolution.SolutionDirectory.CombineWithShortName(ProjectExtensions.AssetsFolder);
                if (!assetsDir.IsAbsolute)
                {
                    myLogger.Warn($"Computed assetsDir {assetsDir} is not absolute. Skipping installation.");
                    return InstallationInfo.DoNotInstall;
                }

                if (!assetsDir.ExistsDirectory)
                {
                    myLogger.Info("No Assets directory in the same directory as solution. Skipping installation.");
                    return InstallationInfo.DoNotInstall;
                }

                var defaultDir = assetsDir
                    .CombineWithShortName("Plugins")
                    .CombineWithShortName("Editor")
                    .CombineWithShortName("JetBrains");

                // default case: all is good, we have cached the installation dir
                if (!previousInstallationDir.IsNullOrEmpty() &&
                    TryFindExistingPluginOnDisk(previousInstallationDir, newVersion, out var installationInfo))
                {
                    return installationInfo;
                }

                // Check the default location
                if (TryFindExistingPluginOnDisk(defaultDir, newVersion, out installationInfo))
                    return installationInfo;

                // dll is there, but was not referenced by any project, for example - only Assembly-CSharp project is present
                if (TryFindExistingPluginOnDiskInFolderRecursive(assetsDir, newVersion, out var installationInfo1))
                {
                    return installationInfo1;
                }

                // not fresh install, but nothing in previously installed dir on in solution
                if (!previousInstallationDir.IsNullOrEmpty())
                {
                    myLogger.Info(
                        "Plugin not found in previous installation dir '{0}' or in solution. Falling back to default directory.",
                        previousInstallationDir);
                }
                else
                {
                    myLogger.Info("Plugin not found in solution. Installing to default location");
                }

                return InstallationInfo.FreshInstall(defaultDir);
            }
            catch (Exception e)
            {
                myLogger.LogExceptionSilently(e);
                return InstallationInfo.DoNotInstall;
            }
        }

        private bool TryFindExistingPluginOnDiskInFolderRecursive(VirtualFileSystemPath directory, Version newVersion, [NotNull] out InstallationInfo result)
        {
            myLogger.Verbose("Looking for plugin on disk: '{0}'", directory);

            var pluginFiles = directory
                .GetChildFiles("*.dll", PathSearchFlags.RecurseIntoSubdirectories)
                .Where(f => f.Name == PluginPathsProvider.BasicPluginDllFile)
                .ToList();

            if (pluginFiles.Count == 0)
            {
                result = InstallationInfo.DoNotInstall;
                return false;
            }

            result = GetInstallationInfoFromFoundInstallation(pluginFiles, newVersion);
            return true;
        }

        private bool TryFindExistingPluginOnDisk(VirtualFileSystemPath directory, Version newVersion, [NotNull] out InstallationInfo result)
        {
            myLogger.Verbose("Looking for plugin on disk: '{0}'", directory);
            var oldPluginFiles = directory
                .GetChildFiles("*.cs")
                .Where(f => PluginCsFile == f.Name)
                .ToList();

            var pluginFiles = directory
                .GetChildFiles("*.dll")
                .Where(f => f.Name == PluginPathsProvider.BasicPluginDllFile)
                .ToList();

            pluginFiles.AddRange(oldPluginFiles);
            if (pluginFiles.Count == 0)
            {
                result = InstallationInfo.DoNotInstall;
                return false;
            }

            result = GetInstallationInfoFromFoundInstallation(pluginFiles, newVersion);
            return true;
        }

        [NotNull]
        private InstallationInfo GetInstallationInfoFromFoundInstallation(List<VirtualFileSystemPath> pluginFiles, Version newVersion)
        {
            var parentDirs = pluginFiles.Select(f => f.Directory).Distinct().ToList();
            if (parentDirs.Count > 1)
            {
                myLogger.Warn("Plugin files detected in more than one directory.");
                return InstallationInfo.FoundProblemWithExistingPlugins(pluginFiles);
            }

            if (parentDirs.Count == 0)
            {
                myLogger.Warn("Plugin files do not have parent directory (?).");
                return InstallationInfo.FoundProblemWithExistingPlugins(pluginFiles);
            }

            var pluginDir = parentDirs[0];

            if (pluginFiles.Count == 1 && pluginFiles[0].Name == PluginPathsProvider.BasicPluginDllFile && pluginFiles[0].ExistsFile)
            {
                try
                {
                    var existingVersion = new Version(FileVersionInfo.GetVersionInfo(pluginFiles[0].FullPath).FileVersion);

                    // Always update to a debug version, even if the versions match
                    if (IsDebugVersion(newVersion))
                        return InstallationInfo.ForceUpdateToDebugVersion(pluginDir, parentDirs, existingVersion);

                    // If the versions are the same, don't update. Note that this means we will also DOWNGRADE if we
                    // load the project in an older version of Rider. This is a good thing - always install the version
                    // of the plugin that the version of Rider is expecting. "Update", not "upgrade"
                    if (newVersion == existingVersion)
                    {
                        myLogger.Verbose($"Plugin v{existingVersion} already installed.");
                        return InstallationInfo.UpToDate(pluginDir, pluginFiles, existingVersion);
                    }

                    return InstallationInfo.ShouldUpdate(pluginDir, pluginFiles, existingVersion);
                }
                catch (Exception)
                {
                    // file may be in Solution-csproj, but doesn't exist on disk
                    return InstallationInfo.ForceUpdate(pluginDir, pluginFiles);
                }
            }

            // update from Unity3dRider.cs to dll or both old and new plugins together
            return InstallationInfo.ForceUpdate(pluginDir, pluginFiles);
        }

        private static bool IsDebugVersion(Version newVersion)
        {
            // Revision is set by the CI, normally. We'll see 9998 if the project is built from the IDE and 9999 if built
            // from the gradle scripts (which means 9999 is a more accurate build)
            return newVersion.Revision == 9999 || newVersion.Revision == 9998;
        }

        public class InstallationInfo
        {
            // Make sure we have four components. The default constructor gives us only major.minor (0.0) and that will
            // fail if we try to compare against a full version (0.0.0.0), which we might get from the file version
            private static readonly Version ourZeroVersion = new Version(0, 0, 0, 0);

            public static readonly InstallationInfo DoNotInstall = new InstallationInfo(InstallReason.DoNotInstall,
                VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext), EmptyArray<VirtualFileSystemPath>.Instance, ourZeroVersion);

            public readonly InstallReason InstallReason;

            public bool ShouldInstallPlugin => !(InstallReason == InstallReason.DoNotInstall || InstallReason == InstallReason.UpToDate);

            [NotNull]
            public readonly VirtualFileSystemPath PluginDirectory;

            [NotNull]
            public readonly ICollection<VirtualFileSystemPath> ExistingFiles;

            [NotNull]
            public readonly Version ExistingVersion;

            private InstallationInfo(InstallReason installReason, [NotNull] VirtualFileSystemPath pluginDirectory,
                [NotNull] ICollection<VirtualFileSystemPath> existingFiles, [NotNull] Version existingVersion)
            {
                var logger = Logger.GetLogger<InstallationInfo>();
                if (!pluginDirectory.IsAbsolute && ShouldInstallPlugin)
                    logger.Error($"pluginDirectory ${pluginDirectory} Is Not Absolute ${installReason}, ${existingVersion}, ${existingFiles.Count}");
                else
                    logger.Info($"pluginDirectory ${pluginDirectory} ${installReason}, ${existingVersion}, ${existingFiles.Count}");

                InstallReason = installReason;
                PluginDirectory = pluginDirectory;
                ExistingFiles = existingFiles;
                ExistingVersion = existingVersion;
            }

            public static InstallationInfo FreshInstall(VirtualFileSystemPath installLocation)
            {
                return new InstallationInfo(InstallReason.FreshInstall, installLocation, EmptyArray<VirtualFileSystemPath>.Instance, ourZeroVersion);
            }

            public static InstallationInfo UpToDate(VirtualFileSystemPath installLocation,
                ICollection<VirtualFileSystemPath> existingPluginFiles, Version existingVersion)
            {
                return new InstallationInfo(InstallReason.UpToDate, installLocation, existingPluginFiles, existingVersion);
            }

            public static InstallationInfo ShouldUpdate(VirtualFileSystemPath installLocation,
                ICollection<VirtualFileSystemPath> existingPluginFiles, Version existingVersion)
            {
                return new InstallationInfo(InstallReason.Update, installLocation, existingPluginFiles, existingVersion);
            }

            public static InstallationInfo ForceUpdate(VirtualFileSystemPath installLocation,
                ICollection<VirtualFileSystemPath> existingPluginFiles)
            {
                return new InstallationInfo(InstallReason.Update, installLocation, existingPluginFiles, ourZeroVersion);
            }

            public static InstallationInfo ForceUpdateToDebugVersion(VirtualFileSystemPath installLocation,
                ICollection<VirtualFileSystemPath> existingPluginFiles, Version existingVersion)
            {
                return new InstallationInfo(InstallReason.ForceUpdateForDebug, installLocation, existingPluginFiles, existingVersion);
            }

            public static InstallationInfo FoundProblemWithExistingPlugins(
                ICollection<VirtualFileSystemPath> existingPluginFiles)
            {
                // We've found a weird situation with existing plugins (found in multiple directories, etc.) don't install
                return new InstallationInfo(InstallReason.DoNotInstall, VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext), existingPluginFiles, ourZeroVersion);
            }
        }

        public enum InstallReason
        {
            DoNotInstall,
            UpToDate,
            FreshInstall,
            Update,
            ForceUpdateForDebug
        }
    }
}