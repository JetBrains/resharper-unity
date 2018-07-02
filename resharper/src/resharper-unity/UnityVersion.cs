using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityVersion
    {
        private readonly UnityProjectFileCacheProvider myUnityProjectFileCache;

        public UnityVersion(UnityProjectFileCacheProvider unityProjectFileCache)
        {
            myUnityProjectFileCache = unityProjectFileCache;
        }

        [NotNull]
        public Version GetActualVersion([CanBeNull] IProject project)
        {
            // Project might be null for e.g. decompiled files
            if (project == null)
                return new Version(0, 0);
            var version = myUnityProjectFileCache.GetUnityVersion(project);
            return version ?? GetActualVersion(project.GetSolution());
        }

        [NotNull]
        private Version GetActualVersion([NotNull] ISolution solution)
        {
            foreach (var project in solution.GetTopLevelProjects())
            {
                if (project.IsUnityProject())
                {
                    var version = myUnityProjectFileCache.GetUnityVersion(project);
                    if (version != null)
                        return version;
                }
            }

            return GetVersionForTests(solution);
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