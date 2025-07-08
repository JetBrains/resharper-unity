using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.HabitatDetector;
using JetBrains.Util;
using JetBrains.Util.Interop;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    public static class UnityInstallationFinder
    {
        private static readonly ILogger ourLogger = Logger.GetLogger(typeof(UnityInstallationFinder));
        
        private const string Tuanjie = "Tuanjie";
        private const string Unity = "Unity";

        [CanBeNull]
        public static UnityInstallationInfo GetApplicationInfo(Version version, UnityVersion unityVersion)
        {
            var possible = GetPossibleInstallationInfos().ToArray();
            var possibleWithVersion = possible.Where(a => a.Version != null).ToList();

            // fast check is we have a best choice
            var bestChoice = TryGetBestChoice(version, possibleWithVersion);
            if (bestChoice != null)
                return bestChoice;

            // best choice not found by version - try version by path then
            var pathForSolution = unityVersion.GetActualAppPathForSolution();
            var versionByAppPath = UnityVersion.GetVersionByAppPath(pathForSolution);
            if (versionByAppPath!=null)
                possibleWithVersion.Add(new UnityInstallationInfo(versionByAppPath, pathForSolution, pathForSolution.FullPath.Contains(Tuanjie)));

            // check best choice again, since newly added version may be best one
            bestChoice = TryGetBestChoice(version, possibleWithVersion);
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

        private static UnityInstallationInfo TryGetBestChoice(Version version, List<UnityInstallationInfo> possibleWithVersion)
        {
            var bestChoice = possibleWithVersion.Where(a =>
                a.Version.Major == version.Major && a.Version.Minor == version.Minor &&
                a.Version.Build == version.Build && a.Version.Revision == version.Revision
            ).OrderBy(b => b.Version).LastOrDefault();
            return bestChoice;
        }

        [NotNull]
        public static VirtualFileSystemPath GetApplicationContentsPath(VirtualFileSystemPath applicationPath)
        {
            if (applicationPath.IsNullOrEmpty())
                return applicationPath;

            AssertApplicationPath(applicationPath);

            switch (PlatformUtil.RuntimePlatform)
            {
                    case JetPlatform.MacOsX:
                        // /Applications/Unity/Hub/Editor/202x.x.xf1/Unity.app/Contents
                        // Note that this path is inside the Unity app, and cannot contain files that can be installed
                        // as separate modules, such as documentation or optional playback engines, as these changes
                        // would break code signing. However, Unity.app/Contents/Documentation is a symlink to
                        // Unity.app/../Documentation and optional playback engines live at Unity.app/../PlaybackEngines
                        // (Unity.app/Content/PlaybackEngines contains MacStandaloneSupport by default)
                        return applicationPath.Combine("Contents");
                    case JetPlatform.Linux:
                    case JetPlatform.Windows:
                        // C:\Program Files\Unity\Hub\Editor\202x.x.xf1\Editor\Data
                        return applicationPath.Directory.Combine("Data");
            }
            ourLogger.Error("Unknown runtime platform");
            return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);
        }

        [NotNull]
        public static VirtualFileSystemPath GetPackageManagerDefaultManifest(VirtualFileSystemPath applicationPath)
        {
            return applicationPath.IsEmpty
                ? applicationPath
                : GetApplicationContentsPath(applicationPath).Combine("Resources/PackageManager/Editor/manifest.json");
        }

        private static List<UnityInstallationInfo> GetPossibleInstallationInfos()
        {
            var installations = GetPossibleApplicationPaths(Unity);
            var unityInstallationsInfo =  installations.Select(path =>
            {
                var version = UnityVersion.GetVersionByAppPath(path);
                return new UnityInstallationInfo(version, path, false);
            }).ToList();
            
            var tuanjieInstallations = GetPossibleApplicationPaths(Tuanjie);
            var tuanjieInstallationsInfo = tuanjieInstallations.Select(path =>
            {
                var version = UnityVersion.GetVersionByAppPath(path);
                return new UnityInstallationInfo(version, path, true);
            }).ToList();

            if (tuanjieInstallationsInfo.Count == 0)
                return unityInstallationsInfo;

            unityInstallationsInfo.AddRange(tuanjieInstallationsInfo);
            return unityInstallationsInfo;
        }
        
        private static List<VirtualFileSystemPath> GetPossibleApplicationPaths(string engineName)
        {
            switch (PlatformUtil.RuntimePlatform)
            {
                case JetPlatform.MacOsX:
                {
                    var appsHome = VirtualFileSystemPath.Parse("/Applications", InteractionContext.SolutionContext);
                    var unityApps = appsHome.GetChildDirectories($"{engineName}*").Select(a=>a.Combine($"{engineName}.app")).ToList();

                    var defaultHubLocation = appsHome.Combine($"{engineName}/Hub/Editor");
                    var hubLocations = new List<VirtualFileSystemPath> {defaultHubLocation};

                    // Hub custom location
                    var home = Environment.GetEnvironmentVariable("HOME");
                    if (!string.IsNullOrEmpty(home))
                    {
                        var localAppData = VirtualFileSystemPath.Parse(home, InteractionContext.SolutionContext).Combine("Library/Application Support");
                        var hubCustomLocation = GetCustomHubInstallPath(engineName, localAppData);
                        if (!hubCustomLocation.IsEmpty)
                            hubLocations.Add(hubCustomLocation);
                    }

                    // /Applications/Unity/Hub/Editor/2018.1.0b4/Unity.app
                    unityApps.AddRange(hubLocations.SelectMany(l=>l.GetChildDirectories().Select(unityDir =>
                        unityDir.Combine($@"{engineName}.app"))));

                    return unityApps.Where(a=>a.ExistsDirectory).Distinct().OrderBy(b=>b.FullPath).ToList();
                }
                case JetPlatform.Linux:
                {
                    var unityApps = new List<VirtualFileSystemPath>();
                    var homeEnv = Environment.GetEnvironmentVariable("HOME");
                    var homes = new List<VirtualFileSystemPath> {VirtualFileSystemPath.Parse("/opt", InteractionContext.SolutionContext)};
                    if (!string.IsNullOrEmpty(homeEnv))
                    {
                        homes.Add(VirtualFileSystemPath.Parse(homeEnv, InteractionContext.SolutionContext));
                    }

                    // Old style installations
                    unityApps.AddRange(
                        homes.SelectMany(a => a.GetChildDirectories($"{engineName}*"))
                            .Select(unityDir => unityDir.Combine($@"Editor/{engineName}")));

                    // Installations with Unity Hub
                    if (!string.IsNullOrEmpty(homeEnv))
                    {
                        var home = VirtualFileSystemPath.Parse(homeEnv, InteractionContext.SolutionContext);
                        var defaultHubLocation = home.Combine($"{engineName}/Hub/Editor");
                        var hubLocations = new List<VirtualFileSystemPath> {defaultHubLocation};
                        // Hub custom location
                        var configPath = home.Combine(".config");
                        var customHubInstallPath = GetCustomHubInstallPath(engineName, configPath);
                        if (!customHubInstallPath.IsEmpty)
                            hubLocations.Add(customHubInstallPath);

                        unityApps.AddRange(hubLocations.SelectMany(l=>l.GetChildDirectories().Select(unityDir =>
                            unityDir.Combine($@"Editor/{engineName}"))));
                    }

                    return unityApps.Where(a=>a.ExistsFile).Distinct().OrderBy(b=>b.FullPath).ToList();
                }

                case JetPlatform.Windows:
                {
                    var unityApps = new List<VirtualFileSystemPath>();

                    var programFiles = GetProgramFiles();
                    unityApps.AddRange(
                        programFiles.GetChildDirectories($"{engineName}*")
                            .Select(unityDir => unityDir.Combine($@"Editor\{engineName}.exe"))
                        );

                    // default Hub location
                    //"C:\Program Files\Unity\Hub\Editor\2018.1.0b4\Editor\Data\MonoBleedingEdge"
                    unityApps.AddRange(
                        programFiles.Combine($@"{engineName}\Hub\Editor").GetChildDirectories()
                            .Select(unityDir => unityDir.Combine($@"Editor\{engineName}.exe"))
                    );

                    // custom Hub location
                    var appData = VirtualFileSystemPath.Parse(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), InteractionContext.SolutionContext);
                    var customHubInstallPath = GetCustomHubInstallPath(engineName, appData);
                    if (!customHubInstallPath.IsEmpty)
                    {
                        unityApps.AddRange(
                            customHubInstallPath.GetChildDirectories()
                                .Select(unityDir => unityDir.Combine($@"Editor\{engineName}.exe"))
                        );
                    }

                    var lnks = VirtualFileSystemPath.Parse(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs", InteractionContext.SolutionContext)
                        .GetChildDirectories($"{engineName}*").SelectMany(a => a.GetChildFiles($"{engineName}.lnk")).ToArray();
                    unityApps.AddRange(lnks
                        .Select(t => ShellLinkHelper.ResolveLinkTarget(t.ToNativeFileSystemPath()).ToVirtualFileSystemPath())
                        .OrderBy(c => new FileInfo(c.FullPath).CreationTime));

                    foreach (var VirtualFileSystemPath in unityApps)
                    {
                        ourLogger.Log(LoggingLevel.VERBOSE, "Possible unity path: " + VirtualFileSystemPath);
                    }
                    return unityApps.Where(a=>a.ExistsFile).Distinct().OrderBy(b=>b.FullPath).ToList();
                }
            }
            ourLogger.Error("Unknown runtime platform");
            return new List<VirtualFileSystemPath>();
        }

        private static VirtualFileSystemPath GetCustomHubInstallPath(string engineName, VirtualFileSystemPath appData)
        {
            var filePath = appData.Combine($"{engineName}Hub/secondaryInstallPath.json");
            if (filePath.ExistsFile)
            {
                var text = filePath.ReadAllText2().Text.TrimStart('"').TrimEnd('"');
                var customHubLocation = VirtualFileSystemPath.Parse(text, InteractionContext.SolutionContext);
                if (customHubLocation.ExistsDirectory)
                    return customHubLocation;
            }
            return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);
        }

        private static VirtualFileSystemPath GetProgramFiles()
        {
            // PlatformUtils.GetProgramFiles() will return the relevant folder for
            // the current app, not the current system. So a 32 bit app on a 64 bit
            // system will return the 32 bit Program Files. Force to get the system
            // native Program Files folder
            var environmentVariable = Environment.GetEnvironmentVariable("ProgramW6432");
            return string.IsNullOrWhiteSpace(environmentVariable) ? VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext) : VirtualFileSystemPath.TryParse(environmentVariable, InteractionContext.SolutionContext);
        }

        [CanBeNull]
        public static VirtualFileSystemPath GetAppPathByDll(XmlElement documentElement)
        {
            var referencePathElement = documentElement.ChildElements()
                .Where(a => a.Name == "ItemGroup").SelectMany(b => b.ChildElements())
                .Where(c => c.Name == "Reference" &&
                            (c.GetAttribute("Include").Equals("UnityEngine") // we can't use StartsWith here, some "UnityEngine*" libs are in packages
                             || c.GetAttribute("Include").Equals("UnityEngine.CoreModule") // Dll project may have this reference instead of UnityEngine.dll
                             || c.GetAttribute("Include").Equals("UnityEditor")))
                .SelectMany(d => d.ChildElements())
                .FirstOrDefault(c => c.Name == "HintPath");

            if (referencePathElement == null || string.IsNullOrEmpty(referencePathElement.InnerText))
                return null;

            var filePath = VirtualFileSystemPath.Parse(referencePathElement.InnerText, InteractionContext.SolutionContext);
            if (!filePath.IsAbsolute) // RIDER-21237
                return null;

            if (filePath.ExistsFile)
            {
                switch (PlatformUtil.RuntimePlatform)
                {
                    case JetPlatform.Windows:
                    {
                        return GoUpForUnityExecutable(filePath,$"{Unity}.exe", path => path.ExistsFile) 
                               ?? GoUpForUnityExecutable(filePath,$"{Tuanjie}.exe", path => path.ExistsFile);
                    }
                    case JetPlatform.Linux:
                    {
                        return GoUpForUnityExecutable(filePath, Unity, path => path.ExistsFile)
                               ?? GoUpForUnityExecutable(filePath, Tuanjie, path => path.ExistsFile);
                    }
                    case JetPlatform.MacOsX:
                    {
                        var result = GoUpForUnityExecutable(filePath, $"{Unity}.app", path => path.ExistsDirectory) 
                                     ?? GoUpForUnityExecutable(filePath, $"{Tuanjie}.app", path => path.ExistsDirectory);
                        // not sure, how this worked before: either older Unity versions or assembly is inside the Unity.app/Contents
                        if (result == null)
                        {
                            var appPath = filePath;
                            while (!appPath.Name.Equals("Contents"))
                            {
                                appPath = appPath.Directory;
                                if (!appPath.ExistsDirectory || appPath.IsEmpty)
                                    return null;
                            }

                            appPath = appPath.Directory;
                            return appPath;    
                        }
                        return result;
                    }
                    default:
                        ourLogger.Error("Unknown runtime platform");
                        break;
                }
            }

            return null;
        }

        private static VirtualFileSystemPath GoUpForUnityExecutable(VirtualFileSystemPath filePath, string targetName, Func<VirtualFileSystemPath, bool> checkExists)
        {
            // For Player Projects it might be: Editor/Data/PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed/UnityEngine.dll
            // For Editor: Editor\Data\Managed\UnityEngine.dll
            // Or // Editor\Data\Managed\UnityEngine\UnityEngine.dll

            var path = filePath;
            while (!path.IsEmpty)
            {
                if (checkExists(path.Combine(targetName)))
                {
                    return path.Combine(targetName);
                }
                path = path.Directory;
            }
            return null;
        }

        // Asserts the given path is to the application itself, either Whatever.app/ for Mac or Whatever.exe for Windows
        [Conditional("JET_MODE_ASSERT")]
        private static void AssertApplicationPath(VirtualFileSystemPath path)
        {
            if (path.IsEmpty || path.Exists == FileSystemPath.Existence.Missing)
                return;

            switch (PlatformUtil.RuntimePlatform)
            {
                case JetPlatform.MacOsX:
                    Assertion.Assert(path.ExistsDirectory, "path.ExistsDirectory");
                    Assertion.Assert(path.FullPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase),
                        "path.FullPath.EndsWith('.app', StringComparison.OrdinalIgnoreCase)");
                    break;
                case JetPlatform.Windows:
                    Assertion.Assert(path.ExistsFile, "path.ExistsFile");
                    Assertion.Assert(path.FullPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase),
                        "path.FullPath.EndsWith('.exe', StringComparison.OrdinalIgnoreCase)");
                    break;
                case JetPlatform.Linux:
                    Assertion.Assert(path.ExistsFile, "path.ExistsFile");
                    break;
                default:
                    Assertion.Fail("Unknown runtime platform");
                    break;
            }
        }
    }

    public class UnityInstallationInfo
    {
        public Version Version { get; }
        public VirtualFileSystemPath Path { get; }
        public bool IsTuanjie { get; }

        public UnityInstallationInfo(Version version, VirtualFileSystemPath path, bool isTuanjie)
        {
            Version = version;
            Path = path;
            IsTuanjie = isTuanjie;
        }
    }
}