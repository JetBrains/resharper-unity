using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginDetector
    {
        public static readonly Version ZeroVersion = new Version();
        
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;
        private static readonly string[] ourPluginCsFile = {"Unity3DRider.cs"};

        public static readonly string PluginDllFile = "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll";

        public static readonly InstallationInfo ShouldNotInstall = new InstallationInfo(false, FileSystemPath.Empty,
            EmptyArray<FileSystemPath>.Instance, ZeroVersion);

        public UnityPluginDetector(ISolution solution, ILogger logger)
        {
            mySolution = solution;
            myLogger = logger;
        }

        [NotNull]
        public InstallationInfo GetInstallationInfo(FileSystemPath previousInstallationDir)
        {
            try
            {
                var assetsDir = mySolution.SolutionFilePath.Directory.CombineWithShortName("Assets");
                if (!assetsDir.IsAbsolute)
                {
                    myLogger.Warn($"Computed assetsDir {assetsDir} is not absolute. Skipping installation.");
                    return ShouldNotInstall;
                }
                
                if (!assetsDir.ExistsDirectory)
                {
                    myLogger.Info("No Assets directory in the same directory as solution. Skipping installation.");
                    return ShouldNotInstall;
                }
                
                var defaultDir = assetsDir
                    .CombineWithShortName("Plugins")
                    .CombineWithShortName("Editor")
                    .CombineWithShortName("JetBrains");

                InstallationInfo result;
                
                var isFirstInstall = previousInstallationDir.IsNullOrEmpty();
                if (isFirstInstall)
                {                    
                    if (TryFindOnDisk(defaultDir, out result))
                    {
                        return result;
                    }

                    // nothing in solution or default directory on first launch.
                    return NotInstalled(defaultDir);
                }

                // default case: all is good, we have cached the installation dir
                if (TryFindOnDisk(previousInstallationDir, out result))
                {
                    return result;
                }
                
                // not fresh install, but nothing in previously installed dir on in solution
                myLogger.Info("Plugin not found in previous installation dir '{0}' or in solution. Falling back to default directory.", previousInstallationDir);
                
                return NotInstalled(defaultDir);
            }
            catch (Exception e)
            {
                myLogger.LogExceptionSilently(e);
                return ShouldNotInstall;
            }
        }

        private bool TryFindOnDisk(FileSystemPath directory, [NotNull] out InstallationInfo result)
        {
            myLogger.Verbose("Looking for plugin on disk: '{0}'", directory);
            var oldPluginFiles = directory
                .GetChildFiles("*.cs")
                .Where(f => ourPluginCsFile.Contains(f.Name))
                .ToList();
            
            var pluginFiles = directory
                .GetChildFiles("*.dll")
                .Where(f => f.Name == PluginDllFile)
                .ToList();
            
            pluginFiles.AddRange(oldPluginFiles);

            if (pluginFiles.Count == 0)
            {
                result = ShouldNotInstall;
                return false;
            }

            result = ExistingInstallation(pluginFiles);
            return true;
        }

        [NotNull]
        private static InstallationInfo NotInstalled(FileSystemPath pluginDir)
        {
            return new InstallationInfo(true, pluginDir, EmptyArray<FileSystemPath>.Instance, ZeroVersion);
        }

        [NotNull]
        private InstallationInfo ExistingInstallation(List<FileSystemPath> pluginFiles)
        {
            var parentDirs = pluginFiles.Select(f => f.Directory).Distinct().ToList();
            if (parentDirs.Count > 1)
            {
                myLogger.Warn("Plugin files detected in more than one directory.");
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, ZeroVersion);
            }

            if (parentDirs.Count == 0)
            {
                myLogger.Warn("Plugin files do not have parent directory (?).");
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, ZeroVersion);
            }

            var pluginDir = parentDirs[0];

            if (pluginFiles.Count == 1 && pluginFiles[0].Name == PluginDllFile)
            {
                var version = new Version(FileVersionInfo.GetVersionInfo(pluginFiles[0].FullPath).FileVersion);
                return new InstallationInfo(version != ZeroVersion, pluginDir, pluginFiles, version);
            }

            // update from Unity3dRider.cs to dll
            // or
            // both old and new plugins together
            return new InstallationInfo(true, pluginDir, pluginFiles, ZeroVersion);
        }

        public class InstallationInfo
        {
            public readonly bool ShouldInstallPlugin;

            [NotNull]
            public readonly FileSystemPath PluginDirectory;

            [NotNull]
            public readonly ICollection<FileSystemPath> ExistingFiles;

            [NotNull]
            public readonly Version Version;

            public InstallationInfo(bool shouldInstallPlugin, [NotNull] FileSystemPath pluginDirectory,
                [NotNull] ICollection<FileSystemPath> existingFiles, [NotNull] Version version)
            {
                ShouldInstallPlugin = shouldInstallPlugin;
                PluginDirectory = pluginDirectory;
                ExistingFiles = existingFiles;
                Version = version;
            }
        }
    }
}