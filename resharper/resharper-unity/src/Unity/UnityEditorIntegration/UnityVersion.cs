using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JetBrains.Annotations;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;
using Vestris.ResourceLib;

namespace JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration
{
    [SolutionComponent]
    public class UnityVersion
    {
        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;
        private readonly ISolution mySolution;
        private readonly IFileSystemTracker myFileSystemTracker;
        private readonly VirtualFileSystemPath mySolutionDirectory;
        private Version myVersionFromProjectVersionTxt;
        private Version myVersionFromEditorInstanceJson;
        private static readonly ILogger ourLogger = Logger.GetLogger<UnityVersion>();

        public readonly ViewableProperty<Version> ActualVersionForSolution = new ViewableProperty<Version>(new Version(0,0));

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache,
            ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime,
            UnitySolutionTracker unitySolutionTracker)
        {
            myUnityProjectFileCache = unityProjectFileCache;
            mySolution = solution;
            myFileSystemTracker = fileSystemTracker;

            // SolutionDirectory isn't absolute in tests, and will throw if used with FileSystemTracker
            mySolutionDirectory = solution.SolutionDirectory;
            if (!mySolutionDirectory.IsAbsolute)
                mySolutionDirectory = solution.SolutionDirectory.ToAbsolutePath(FileSystemUtil.GetCurrentDirectory().ToVirtualFileSystemPath());

            unitySolutionTracker.IsUnityProjectFolder.WhenTrue(lifetime, SetActualVersionForSolution);
        }

        private void SetActualVersionForSolution(Lifetime lt)
        {
            var projectVersionTxtPath =
                mySolutionDirectory.Combine("ProjectSettings/ProjectVersion.txt");
            myFileSystemTracker.AdviseFileChanges(lt,
                projectVersionTxtPath,
                _ =>
                {
                    myVersionFromProjectVersionTxt = TryGetVersionFromProjectVersion(projectVersionTxtPath);
                    UpdateActualVersionForSolution();
                });
            myVersionFromProjectVersionTxt = TryGetVersionFromProjectVersion(projectVersionTxtPath);

            var editorInstanceJsonPath = mySolutionDirectory.Combine("Library/EditorInstance.json");
            myFileSystemTracker.AdviseFileChanges(lt,
                editorInstanceJsonPath,
                _ =>
                {
                    myVersionFromEditorInstanceJson =
                        TryGetApplicationPathFromEditorInstanceJson(editorInstanceJsonPath);
                    UpdateActualVersionForSolution();
                });
            myVersionFromEditorInstanceJson =
                TryGetApplicationPathFromEditorInstanceJson(editorInstanceJsonPath);

            UpdateActualVersionForSolution();
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
        private Version GetActualVersionForSolution()
        {
            if (myVersionFromEditorInstanceJson != null)
                return myVersionFromEditorInstanceJson;
            if (myVersionFromProjectVersionTxt != null)
                return myVersionFromProjectVersionTxt;

            if (mySolution.IsVirtualSolution())
                return new Version(0, 0);

            foreach (var project in GetTopLevelProjectWithReadLock(mySolution))
            {
                var version = myUnityProjectFileCache.GetUnityVersion(project);
                if (version != null)
                    return version;
            }

            return GetVersionForTests(mySolution);
        }

        [NotNull]
        public VirtualFileSystemPath GetActualAppPathForSolution()
        {
            if (mySolution.IsVirtualSolution())
                return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);

            foreach (var project in GetTopLevelProjectWithReadLock(mySolution))
            {
                var path = myUnityProjectFileCache.GetAppPath(project);
                    if (path != null)
                        return path;
            }
            return VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext);
        }

        [CanBeNull]
        private Version TryGetApplicationPathFromEditorInstanceJson(VirtualFileSystemPath editorInstanceJsonPath)
        {
            var val = EditorInstanceJson.TryGetValue(editorInstanceJsonPath, "version");
            return val != null ? Parse(val) : null;
        }

        [CanBeNull]
        private Version TryGetVersionFromProjectVersion(VirtualFileSystemPath projectVersionTxtPath)
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
                    // Get the version define from the project configuration, if set. The solution might be initialised
                    // before the test aspect attribute has a chance to update the project configuration, so fall back
                    // to the properties collection.
                    var defineConstants = configuration.DefineConstants ?? string.Empty;
                    unityVersion = UnityProjectFileCacheProvider.GetVersionFromDefines(defineConstants, unityVersion);
                    if (unityVersion.Major == 0)
                    {
                        configuration.PropertiesCollection.TryGetValue("DefineConstants", out var defineConstantsProp);
                        unityVersion =
                            UnityProjectFileCacheProvider.GetVersionFromDefines(defineConstantsProp ?? string.Empty,
                                unityVersion);
                    }
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
            if (string.IsNullOrEmpty(input))
                return null;

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
            if (version == null)
                throw new ArgumentNullException(nameof(version));

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

            var build = version.Build >= 0 ? $".{version.Build}" : string.Empty;
            return $"{version.Major}.{version.Minor}{build}{type}{rev}";
        }

        public static bool RequiresRiderPackage(Version version)
        {
            return version >= new Version(2019,2);
        }

        private static ConcurrentDictionary<VirtualFileSystemPath, Version> myUnityPathToVersion = new ConcurrentDictionary<VirtualFileSystemPath, Version>();

        public static Version GetVersionByAppPath(VirtualFileSystemPath appPath)
        {
            if (appPath == null || appPath.Exists == FileSystemPath.Existence.Missing)
                return null;

            return myUnityPathToVersion.GetOrAdd(appPath, p => GetVersionByAppPathInternal(p));
        }

        private static Version GetVersionByAppPathInternal(VirtualFileSystemPath appPath)
        {
            Version version = null;
            ourLogger.CatchWarn(() => // RIDER-23674
            {
                switch (PlatformUtil.RuntimePlatform)
                {
                    case PlatformUtil.Platform.Windows:

                        ourLogger.CatchWarn(() =>
                        {
                            var fileVersion = FileVersionInfo.GetVersionInfo(appPath.FullPath).FileVersion;
                            if (!string.IsNullOrEmpty(fileVersion))
                                version = Version.Parse(Version.Parse(fileVersion).ToString(3));
                        });

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

        public void UpdateActualVersionForSolution()
        {
            ActualVersionForSolution.SetValue(GetActualVersionForSolution());
        }
    }
}