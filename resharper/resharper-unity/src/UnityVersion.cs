using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JetBrains.Annotations;
using JetBrains.Application.FileSystemTracker;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityVersion
    {
        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;
        private readonly ISolution mySolution;
        private Version myVersionFromProjectVersion;
        private static string pattern = @"(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)(?<type>[a-z])(?<revision>\d+)";

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache, ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime)
        {
            myUnityProjectFileCache = unityProjectFileCache;
            mySolution = solution;

            var projectVersionTxtPath = mySolution.SolutionDirectory.Combine("ProjectSettings/ProjectVersion.txt");
            fileSystemTracker.AdviseFileChanges(lifetime,
                projectVersionTxtPath,
                _ => { myVersionFromProjectVersion = TryGetVersionFromProjectVersion(projectVersionTxtPath); });
            
            myVersionFromProjectVersion = TryGetVersionFromProjectVersion(projectVersionTxtPath);
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
            if (myVersionFromProjectVersion != null)
                return myVersionFromProjectVersion;
            
            foreach (var project in mySolution.GetTopLevelProjects())
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
        
        [CanBeNull]
        private Version TryGetVersionFromProjectVersion(FileSystemPath projectVersionTxt)
        {
            // Get the version from ProjectSettings/ProjectVersion.txt
            if (!projectVersionTxt.ExistsFile)
                return null;
            var text = projectVersionTxt.ReadAllText2().Text;
            var match = Regex.Match(text, "m_EditorVersion: (?<version>.*$)");
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
            foreach (var project in solution.GetTopLevelProjects())
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

        public static Version Parse(string input)
        {
            var match = Regex.Match(input, pattern);
            var groups = match.Groups;
            Version version = null;
            if (match.Success)
            {
                version = Version.Parse($"{groups["major"].Value}.{groups["minor"].Value}.{groups["build"].Value}");
            }

            return version;
        }

        public static string GetVersionFromInfoPlist(FileSystemPath infoPlistPath)
        {
            var docs = XDocument.Load(infoPlistPath.FullPath);
            var keyValuePairs = docs.Descendants("dict")
                .SelectMany(d => d.Elements("key").Zip(d.Elements().Where(e => e.Name != "key"), (k, v) => new { Key = k, Value = v }))
                .ToDictionary(i => i.Key.Value, i => i.Value.Value);
            var fullVersion = keyValuePairs["CFBundleVersion"];
            return fullVersion;
        }
    }
}