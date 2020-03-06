using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JetBrains.Annotations;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Vestris.ResourceLib;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityVersion
    {
        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;
        private readonly ISolution mySolution;
        private Version myVersionFromProjectVersionTxt;
        private Version myVersionFromEditorInstanceJson;
        private static readonly ILogger ourLogger = Logger.GetLogger<UnityVersion>();

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache,
            ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime, bool inTests = false)
        {
            myUnityProjectFileCache = unityProjectFileCache;
            mySolution = solution;

            if (inTests)
                return;

            var projectVersionTxtPath = mySolution.SolutionDirectory.Combine("ProjectSettings/ProjectVersion.txt");
            fileSystemTracker.AdviseFileChanges(lifetime,
                projectVersionTxtPath,
                _ => { myVersionFromProjectVersionTxt = TryGetVersionFromProjectVersion(projectVersionTxtPath); });
            myVersionFromProjectVersionTxt = TryGetVersionFromProjectVersion(projectVersionTxtPath);

            var editorInstanceJsonPath = mySolution.SolutionDirectory.Combine("Library/EditorInstance.json");
            fileSystemTracker.AdviseFileChanges(lifetime,
                editorInstanceJsonPath,
                _ => { myVersionFromEditorInstanceJson = TryGetApplicationPathFromEditorInstanceJson(editorInstanceJsonPath); });
            myVersionFromEditorInstanceJson = TryGetApplicationPathFromEditorInstanceJson(editorInstanceJsonPath);
        }

        [NotNull]
        public Version GetActualVersion([CanBeNull] IProject project)
        {
            // Project might be null for e.g. decompiled files
            if (project == null)
                return new Version(0, 0);
            var version = myUnityProjectFileCache.GetUnityVersion(project);
            return version ?? GetActualVersionForSolution();
        }

        [NotNull]
        public Version GetActualVersionForSolution()
        {
            if (myVersionFromEditorInstanceJson != null)
                return myVersionFromEditorInstanceJson;
            if (myVersionFromProjectVersionTxt != null)
                return myVersionFromProjectVersionTxt;

            if (mySolution.IsVirtualSolution())
                return new Version(0, 0);

            foreach (var project in GetTopLevelProjectWithReadLock(mySolution))
            {
                if (project.IsUnityProject())
                {
                    var version = myUnityProjectFileCache.GetUnityVersion(project);
                    if (version != null)
                        return version;
                }
            }

            return GetVersionForTests(mySolution);
        }

        [NotNull]
        public FileSystemPath GetActualAppPathForSolution()
        {
            if (mySolution.IsVirtualSolution())
                return FileSystemPath.Empty;

            foreach (var project in GetTopLevelProjectWithReadLock(mySolution))
            {
                if (project.IsUnityProject())
                {
                    var path = myUnityProjectFileCache.GetAppPath(project);
                    if (path != null)
                        return path;
                }
            }
            return FileSystemPath.Empty;
        }

        [CanBeNull]
        private Version TryGetApplicationPathFromEditorInstanceJson(FileSystemPath editorInstanceJsonPath)
        {
            var val = EditorInstanceJson.TryGetValue(editorInstanceJsonPath, "version");
            return val != null ? Parse(val) : null;
        }

        [CanBeNull]
        private Version TryGetVersionFromProjectVersion(FileSystemPath projectVersionTxtPath)
        {
            // Get the version from ProjectSettings/ProjectVersion.txt
            if (!projectVersionTxtPath.ExistsFile)
                return null;
            var text = projectVersionTxtPath.ReadAllText2().Text;
            var match = Regex.Match(text, @"^m_EditorVersion:\s+(?<version>.*)\s*$", RegexOptions.Multiline);
            var groups = match.Groups;
            return match.Success ? Parse(groups["version"].Value) : null;
        }

        private static Version GetVersionForTests(ISolution solution)
        {
            // The project file data provider/cache doesn't work in tests, because there is no .csproj file we can parse.
            // Instead, pull the version directly from the project defines in the project model. We can't rely on this
            // as our main strategy because Unity doesn't write defines for Release configuration (another reason we for
            // us to hide the project configuration selector)
            var unityVersion = new Version(0, 0);
            foreach (var project in GetTopLevelProjectWithReadLock(solution))
            {
                foreach (var configuration in project.ProjectProperties.GetActiveConfigurations<IManagedProjectConfiguration>())
                {
                    // Get the constants. The tests can't set this up correctly, so they
                    // add the Unity version as a property
                    var defineConstants = configuration.DefineConstants;
                    if (string.IsNullOrEmpty(defineConstants))
                        configuration.PropertiesCollection.TryGetValue("DefineConstants", out defineConstants);

                    unityVersion = UnityProjectFileCacheProvider.GetVersionFromDefines(defineConstants ?? string.Empty,
                        unityVersion);
                }
            }

            return unityVersion;
        }

        private static ICollection<IProject> GetTopLevelProjectWithReadLock(ISolution solution)
        {
            ICollection<IProject> projects;
            using (ReadLockCookie.Create())
            {
                projects = solution.GetTopLevelProjects();
            }

            return projects;
        }

        [CanBeNull]
        public static Version Parse(string input)
        {
            const string pattern = @"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(?<type>[a-z])(?<revision>\d+)";
            var match = Regex.Match(input, pattern);
            var groups = match.Groups;
            Version version = null;
            if (match.Success)
            {
                var typeWithRevision = "0";
                try
                {
                    var typeChar = groups["type"].Value.ToCharArray()[0];
                    var shiftedChar = 16 + typeChar; // Because `f1` = `1021` and `b10` = `9810`, which will break sorting
                    var revision = Convert.ToInt32(groups["revision"].Value);
                    typeWithRevision = shiftedChar.ToString("D3") + revision.ToString("D3");
                }
                catch (Exception e)
                {
                    ourLogger.Error($"Unable to parse part of version. type={groups["type"].Value} revision={groups["revision"].Value}", e);
                }

                version = Version.Parse($"{groups["major"].Value}.{groups["minor"].Value}.{groups["build"].Value}.{typeWithRevision}");
            }

            return version;
        }

        public static string VersionToString([NotNull] Version version)
        {
            var type = string.Empty;
            var rev = string.Empty;
            try
            {
                var revisionString = version.Revision.ToString(); // first 3 is char, next 1+ ones - revision
                if (revisionString.Length > 3)
                {
                    var charValue = Convert.ToInt32(revisionString.Substring(0, 3)) - 16;
                    type = ((char)charValue).ToString();
                    rev = Convert.ToInt32(revisionString.Substring(3)).ToString();
                }
            }
            catch (Exception e)
            {
                ourLogger.Error($"Unable do VersionToString. Input version={version}", e);
            }

            return $"{version.Major}.{version.Minor}.{version.Build}{type}{rev}";
        }
        
        internal static bool RequiresRiderPackage(Version version)
        {
            return version >= new Version(2019,2);
        }

        public static Version GetVersionByAppPath(FileSystemPath appPath)
        {
            if (appPath == null || appPath.Exists == FileSystemPath.Existence.Missing)
                return null;

            Version version = null;
            ourLogger.CatchWarn(() => // RIDER-23674
            {
                switch (PlatformUtil.RuntimePlatform)
                {
                    case PlatformUtil.Platform.Windows:

                        version = new Version(new Version(FileVersionInfo.GetVersionInfo(appPath.FullPath).FileVersion)
                            .ToString(3));

                        var resource = new VersionResource();
                        resource.LoadFrom(appPath.FullPath);
                        var unityVersionList = resource.Resources.Values.OfType<StringFileInfo>()
                            .Where(c => c.Default.Strings.Keys.Any(b => b == "Unity Version")).ToArray();
                        if (unityVersionList.Any())
                        {
                            var unityVersion = unityVersionList.First().Default.Strings["Unity Version"].StringValue;
                            version = Parse(unityVersion);
                        }

                        break;
                    case PlatformUtil.Platform.MacOsX:
                        var infoPlistPath = appPath.Combine("Contents/Info.plist");
                        if (infoPlistPath.ExistsFile)
                        {
                            var docs = XDocument.Load(infoPlistPath.FullPath);
                            var keyValuePairs = docs.Descendants("dict")
                                .SelectMany(d => d.Elements("key").Zip(d.Elements().Where(e => e.Name != "key"),
                                    (k, v) => new {Key = k, Value = v}))
                                .GroupBy(x => x.Key.Value)
                                .Select(g =>
                                    g.First()) // avoid exception An item with the same key has already been added.
                                .ToDictionary(i => i.Key.Value, i => i.Value.Value);
                            version = Parse(keyValuePairs["CFBundleVersion"]);
                        }

                        break;
                    case PlatformUtil.Platform.Linux:
                        version = Parse(appPath.FullPath); // parse from path
                        break;
                }
            });
            return version;
        }
    }
}