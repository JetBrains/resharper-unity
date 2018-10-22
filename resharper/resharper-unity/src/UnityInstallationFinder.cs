using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Application;
using JetBrains.Util;
using JetBrains.Util.Interop;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class UnityInstallationFinder
    {
        private readonly ILogger myLogger;
        private string pattern = @"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(?<type>[a-z])(?<revision>\d+)";

        public UnityInstallationFinder(ILogger logger)
        {
            myLogger = logger;
        }

        public FileSystemPath GetApplicationPath(Version version)
        {
            var possible = GetPossibleInstallationInfos();
            
            var bestChoice = possible.FirstOrDefault(a =>
                a.Version.Major == version.Major && a.Version.Minor == version.Minor &&
                a.Version.Build == version.Build);
            if (bestChoice != null)
                return bestChoice.Path;
            var secondChoice = possible.FirstOrDefault(a =>
                a.Version.Major == version.Major && a.Version.Minor == version.Minor);
            if (secondChoice != null)
                return secondChoice.Path;
            var worstChoice =  possible.FirstOrDefault(a =>
                a.Version.Major == version.Major);
            return worstChoice?.Path;
        }
        
        public FileSystemPath GetApplicationContentsPath(Version version)
        {
            var applicationPath = GetApplicationPath(version);
            if (applicationPath == null)
                return null;
            switch (PlatformUtil.RuntimePlatform)
            {
                    case PlatformUtil.Platform.MacOsX:
                        return applicationPath.Combine("Contents");
                    case PlatformUtil.Platform.Linux:
                    case PlatformUtil.Platform.Windows:
                        return applicationPath.Directory.Combine("Data");
            }
            myLogger.Error("Unknown runtime platform");
            return null;
        }

        public List<UnityInstallationInfo> GetPossibleInstallationInfos()
        {
            var installations = GetPossibleApplicationPaths();
            return installations.Select(a =>
            {
                var match = Regex.Match(a.FullPath, pattern);
                var groups = match.Groups;
                Version version = null;
                string versionPath = null;
                if (match.Success)
                {
                    versionPath = match.Value;
                    version = Version.Parse($"{groups["major"].Value}.{groups["minor"].Value}.{groups["build"].Value}");
                }
                
                if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
                {
                    version = new Version(new Version(FileVersionInfo.GetVersionInfo(a.FullPath).FileVersion).ToString(3));        
                }
                else if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX)
                {
                    // todo: also possible for Mac
                }
                
                return new UnityInstallationInfo(version, versionPath, a);
            }).ToList();
        }
        
        public List<FileSystemPath> GetPossibleApplicationPaths()
        {
            switch (PlatformUtil.RuntimePlatform)
            {
                case PlatformUtil.Platform.MacOsX:
                {
                    var appsHome = FileSystemPath.Parse("/Applications");
                    var unityApps = appsHome.GetChildDirectories("Unity*").Select(a=>a.Combine("Unity.app")).ToList();

                    var defaultHubLocation = appsHome.Combine("Unity/Hub/Editor");
                    var hubLocations = new List<FileSystemPath> {defaultHubLocation};
                    
                    // Hub custom location
                    var appData = FileSystemPath.Parse(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                    hubLocations.Add(GetCustomHubInstallPath(appData));

                    // /Applications/Unity/Hub/Editor/2018.1.0b4/Unity.app
                    unityApps.AddRange(hubLocations.SelectMany(l=>l.GetChildDirectories().Select(unityDir =>
                        unityDir.Combine(@"Unity.app"))));

                    return unityApps.Where(a=>a.ExistsDirectory).ToList();
                }
                case PlatformUtil.Platform.Linux:
                {
                    var unityApps = new List<FileSystemPath>();
                    var homeEnv = Environment.GetEnvironmentVariable("HOME");
                    var homes = new List<FileSystemPath> {FileSystemPath.Parse("/opt")};
                    
                    unityApps.AddRange(
                        homes.SelectMany(a => a.GetChildDirectories("Unity*"))
                            .Select(unityDir => unityDir.Combine(@"Editor/Unity")));

                    if (homeEnv == null)
                        return unityApps;
                    var home = FileSystemPath.Parse(homeEnv);
                    homes.Add(home);
                    
                    var defaultHubLocation = home.Combine("Unity/Hub/Editor");
                    var hubLocations = new List<FileSystemPath> {defaultHubLocation};
                    // Hub custom location
                    var configPath = home.Combine(".config");
                    hubLocations.Add(GetCustomHubInstallPath(configPath));

                    unityApps.AddRange(hubLocations.SelectMany(l=>l.GetChildDirectories().Select(unityDir =>
                        unityDir.Combine(@"Editor/Unity"))));
                    
                    return unityApps.Where(a=>a.ExistsFile).ToList();
                }

                case PlatformUtil.Platform.Windows:
                {
                    var unityApps = new List<FileSystemPath>();

                    var programFiles = GetProgramFiles();
                    unityApps.AddRange(
                        programFiles.GetChildDirectories("Unity*")
                            .Select(unityDir => unityDir.Combine(@"Editor\Unity.exe"))
                        );
                    
                    // default Hub location
                    //"C:\Program Files\Unity\Hub\Editor\2018.1.0b4\Editor\Data\MonoBleedingEdge" 
                    unityApps.AddRange(
                        programFiles.Combine(@"Unity\Hub\Editor").GetChildDirectories()
                            .Select(unityDir => unityDir.Combine(@"Editor\Unity.exe"))
                    );
                    
                    // custom Hub location
                    var appData = FileSystemPath.Parse(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                    var customHubInstallPath = GetCustomHubInstallPath(appData);
                    unityApps.AddRange(
                        customHubInstallPath.GetChildDirectories()
                            .Select(unityDir => unityDir.Combine(@"Editor\Unity.exe"))
                    );
                    
                    var lnks = FileSystemPath.Parse(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs")
                        .GetChildDirectories("Unity*").SelectMany(a => a.GetChildFiles("Unity.lnk")).ToArray();
                    unityApps.AddRange(lnks
                        .Select(ShellLinkHelper.ResolveLinkTarget)
                        .OrderBy(c => new FileInfo(c.FullPath).CreationTime));

                    return unityApps.Where(a=>a.ExistsFile).ToList();
                }
            }
            myLogger.Error("Unknown runtime platform");
            return new List<FileSystemPath>();
        }

        private static FileSystemPath GetCustomHubInstallPath(FileSystemPath appData)
        {
            var filePath = appData.Combine("UnityHub/secondaryInstallPath.json");
            if (filePath.ExistsFile)
            {
                var customHubLocation = FileSystemPath.Parse(filePath.ReadAllText2().Text);
                if (customHubLocation.ExistsDirectory)
                    return customHubLocation;
            }
            return FileSystemPath.Empty;
        }


        private static FileSystemPath GetProgramFiles()
        {
            // PlatformUtils.GetProgramFiles() will return the relevant folder for
            // the current app, not the current system. So a 32 bit app on a 64 bit
            // system will return the 32 bit Program Files. Force to get the system
            // native Program Files folder
            var environmentVariable = Environment.GetEnvironmentVariable("ProgramW6432");
            return string.IsNullOrWhiteSpace(environmentVariable) ? FileSystemPath.Empty : FileSystemPath.TryParse(environmentVariable);
        }
    }

    public class UnityInstallationInfo
    {
        public Version Version { get; }
        public string PathVersion { get; }
        public FileSystemPath Path { get; }

        public UnityInstallationInfo(Version version, string pathVersion, FileSystemPath path)
        {
            Version = version;
            PathVersion = pathVersion;
            Path = path;
        }
    }
}