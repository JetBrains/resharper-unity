using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.Util;
using JetBrains.Util.Interop;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [ShellComponent]
    public class UnityInstallationFinder
    {
        private readonly ILogger myLogger;

        public UnityInstallationFinder(ILogger logger)
        {
            myLogger = logger;
        }

        public UnityInstallationInfo GetApplicationInfo(Version version)
        {
            var possible = GetPossibleInstallationInfos().ToArray();
            var possibleWithVersion = possible.Where(a => a.Version != null).ToArray();
            
            var bestChoice = possibleWithVersion.Where(a =>
                a.Version.Major == version.Major && a.Version.Minor == version.Minor &&
                a.Version.Build == version.Build && a.Version.Revision == version.Revision
                ).OrderBy(b=>b.Version).LastOrDefault();
            if (bestChoice != null)
                return bestChoice;
            var choice1 = possibleWithVersion.Where(a =>
                a.Version.Major == version.Major && a.Version.Minor == version.Minor &&  
                a.Version.Build == version.Build).OrderBy(b=>b.Version).LastOrDefault();
            if (choice1 != null)
                return choice1;
            var choice2 = possibleWithVersion.Where(a =>
                a.Version.Major == version.Major && a.Version.Minor == version.Minor).OrderBy(b=>b.Version).LastOrDefault();
            if (choice2 != null)
                return choice2;
            var choice3 =  possibleWithVersion.Where(a => a.Version.Major == version.Major)
                .OrderBy(b=>b.Version).LastOrDefault();
            if (choice3!=null)
                return choice3;
            var choice4 =  possibleWithVersion
                .OrderBy(b=>b.Version).LastOrDefault();
            if (choice4!=null)
                return choice4;
            
            var worstChoice = possible.LastOrDefault();
            return worstChoice;
        }
        
        public FileSystemPath GetApplicationContentsPath(Version version)
        {
            var applicationPath = GetApplicationInfo(version).Path;
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
            return installations.Select(path =>
            {
                Version version = null;
                switch (PlatformUtil.RuntimePlatform)
                {
                    case PlatformUtil.Platform.Windows:
                        version = UnityVersion.ReadUnityVersionFromExe(path);
                        break;
                    case PlatformUtil.Platform.MacOsX:
                        var infoPlistPath = path.Combine("Contents/Info.plist");
                        version = UnityVersion.GetVersionFromInfoPlist(infoPlistPath);
                        break;
                    case PlatformUtil.Platform.Linux:
                        version = UnityVersion.Parse(path.FullPath); // parse from path
                        break;
                }
                
                return new UnityInstallationInfo(version, path);
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
                    var hubCustomLocation = GetCustomHubInstallPath(appData);
                    if (!hubCustomLocation.IsEmpty)
                        hubLocations.Add(hubCustomLocation);

                    // /Applications/Unity/Hub/Editor/2018.1.0b4/Unity.app
                    unityApps.AddRange(hubLocations.SelectMany(l=>l.GetChildDirectories().Select(unityDir =>
                        unityDir.Combine(@"Unity.app"))));

                    return unityApps.Where(a=>a.ExistsDirectory).Distinct().OrderBy(b=>b.FullPath).ToList();
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
                    var customHubInstallPath = GetCustomHubInstallPath(configPath);
                    if (!customHubInstallPath.IsEmpty)
                        hubLocations.Add(customHubInstallPath);

                    unityApps.AddRange(hubLocations.SelectMany(l=>l.GetChildDirectories().Select(unityDir =>
                        unityDir.Combine(@"Editor/Unity"))));
                    
                    return unityApps.Where(a=>a.ExistsFile).Distinct().OrderBy(b=>b.FullPath).ToList();
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
                    if (!customHubInstallPath.IsEmpty)
                    {
                        unityApps.AddRange(
                            customHubInstallPath.GetChildDirectories()
                                .Select(unityDir => unityDir.Combine(@"Editor\Unity.exe"))
                        );
                    }
                    
                    var lnks = FileSystemPath.Parse(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs")
                        .GetChildDirectories("Unity*").SelectMany(a => a.GetChildFiles("Unity.lnk")).ToArray();
                    unityApps.AddRange(lnks
                        .Select(ShellLinkHelper.ResolveLinkTarget)
                        .OrderBy(c => new FileInfo(c.FullPath).CreationTime));

                    foreach (var fileSystemPath in unityApps)
                    {
                        myLogger.Log(LoggingLevel.VERBOSE, "Possible unity path: " + fileSystemPath);
                    }
                    return unityApps.Where(a=>a.ExistsFile).Distinct().OrderBy(b=>b.FullPath).ToList();
                    
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
                var text = filePath.ReadAllText2().Text.TrimStart('"').TrimEnd('"');
                var customHubLocation = FileSystemPath.Parse(text);
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
        public FileSystemPath Path { get; }

        public UnityInstallationInfo(Version version, FileSystemPath path)
        {
            Version = version;
            Path = path;
        }
    }
}