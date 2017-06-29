#if RIDER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityPluginDetector
    {
        private readonly ISolution mySolution;
        private readonly ILogger myLogger;
        private static readonly string[] ourPluginFilesV180 = {"RiderAssetPostprocessor.cs", "RiderPlugin.cs"};

        public static readonly string MergedPluginFile = "Unity3DRider.cs";

        private static readonly Regex ourVersionRegex = new Regex(@"// ((?:[0-9]\.)+[0-9])", RegexOptions.Compiled);

        public static readonly InstallationInfo ShouldNotInstall = new InstallationInfo(false, FileSystemPath.Empty,
            EmptyArray<FileSystemPath>.Instance, new Version());

        public UnityPluginDetector(ISolution solution, ILogger logger)
        {
            mySolution = solution;
            myLogger = logger;
        }

        public InstallationInfo GetInstallationInfo(ICollection<IProject> unityProjects, FileSystemPath installationDir)
        {
            try
            {
                var assetsDir = mySolution.SolutionFilePath.Directory.CombineWithShortName("Assets");
                if (!assetsDir.ExistsDirectory)
                {
                    myLogger.Info("No Assets directory in the same directory as solution. Skipping installation.");
                    return ShouldNotInstall;
                }

                if (!installationDir.IsNullOrEmpty())
                {
                    var installation = DetectExistingInstallation(installationDir);
                    if (installation != null)
                        return installation;
                }
                
                foreach (var project in unityProjects)
                {
                    var installation = DetectExistingInstallation(project);
                    if (installation != null)
                        return installation;
                }

                if (installationDir.IsNullOrEmpty())
                {
                    installationDir = assetsDir
                        .CombineWithShortName("Plugins")
                        .CombineWithShortName("Editor")
                        .CombineWithShortName("JetBrains");
                }

                if (!installationDir.ExistsDirectory)
                    return NotInstalled(installationDir);

                var existingFiles = installationDir
                    .GetChildFiles("*.cs")
                    .Where(f => ourPluginFilesV180.Contains(f.Name) || f.Name == MergedPluginFile)
                    .ToList();

                if (existingFiles.Count == 0)
                    return NotInstalled(installationDir);

                return ExistingInstallation(existingFiles);
            }
            catch (Exception e)
            {
                myLogger.LogExceptionSilently(e);
                return ShouldNotInstall;
            }
        }

        [CanBeNull]
        private InstallationInfo DetectExistingInstallation(IProject project)
        {
            var pluginFiles = project
                .GetAllProjectFiles(f =>
                {
                    var location = f.Location;
                    if (location == null || !location.ExistsFile) return false;

                    var fileName = location.Name;
                    return ourPluginFilesV180.Contains(fileName) || fileName == MergedPluginFile;
                })
                .Select(f => f.Location)
                .ToList();

            if (pluginFiles.Count == 0)
                return null;

            return ExistingInstallation(pluginFiles);
        }
        
        [CanBeNull]
        private InstallationInfo DetectExistingInstallation(FileSystemPath directory)
        {
            var pluginFiles = directory
                .GetChildFiles("*.cs")
                .Where(f => ourPluginFilesV180.Contains(f.Name) || f.Name == MergedPluginFile)
                .ToList();

            if (pluginFiles.Count == 0)
                return null;

            return ExistingInstallation(pluginFiles);
        }

        private static InstallationInfo NotInstalled(FileSystemPath pluginDir)
        {
            return new InstallationInfo(true, pluginDir, EmptyArray<FileSystemPath>.Instance, new Version());
        }

        private InstallationInfo ExistingInstallation(List<FileSystemPath> pluginFiles)
        {
            var parentDirs = pluginFiles.Select(f => f.Directory).Distinct().ToList();
            if (parentDirs.Count > 1)
            {
                myLogger.Warn("Plugin files detected in more than one directory.");
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, new Version(0, 0));
            }

            if (parentDirs.Count == 0)
            {
                myLogger.Warn("Plugin files do not have parent directory (?).");
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, new Version(0, 0));
            }

            // v1.8 is two files, v1.9 is one
            if (pluginFiles.Count == 0 || pluginFiles.Count > 2)
            {
                myLogger.Warn("Unsupported plugin file count: {0}", pluginFiles.Count);
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, new Version(0, 0));
            }
            
            var pluginDir = parentDirs[0];
            var filenames = pluginFiles.Select(f => f.Name).ToList();

            // v1.9.0+
            if (pluginFiles.Count == 1)
            {
                if (pluginFiles[0].Name == MergedPluginFile)
                {
                    var version = GetVersionFromFile(pluginFiles[0]);
                    return new InstallationInfo(version.Major > 0, pluginDir, pluginFiles, version);
                }
                
                myLogger.Warn("One file found, but filename is not the same as v1.9.0+");
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, new Version());
            }
            
            // v1.8 probably
            if (filenames.Count == 2)
            {
                if (filenames.IsEquivalentTo(ourPluginFilesV180))
                {
                    return new InstallationInfo(true, pluginDir, pluginFiles, new Version(1, 8, 0, 0));
                }

                myLogger.Warn("Two files found, but filenames are not the same as in v1.8");
                return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, new Version());
            }
            
            return new InstallationInfo(false, FileSystemPath.Empty, pluginFiles, new Version());
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
#endif