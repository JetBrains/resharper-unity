using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
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

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache, ISolution solution)
        {
            myUnityProjectFileCache = unityProjectFileCache;
            mySolution = solution;
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
        private Version TryGetVersionFromProjectVersion()
        {
            // Get the version from ProjectSettings/ProjectVersion.txt
            var projectVersionTxt = mySolution.SolutionDirectory.Combine("ProjectSettings/ProjectVersion.txt");
            if (!projectVersionTxt.ExistsFile)
                return null;
            var text = projectVersionTxt.ReadAllText2().Text;
            var match = Regex.Match(text, "m_EditorVersion: (?<version>.*$)");
            var groups = match.Groups;
            return match.Success ? Version.Parse(groups["version"].Value) : null;
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
    }
}