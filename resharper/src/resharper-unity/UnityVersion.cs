using System;
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

        public Version GetActualVersion(IProject project)
        {
            var version = myUnityProjectFileCache.GetUnityVersion(project);
            return version ?? GetActualVersion(project.GetSolution());
        }

        private Version GetActualVersion(ISolution solution)
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

            // Tests don't create a .csproj we can parse, so pull the version out
            // of the project defines directly (we can't do this normally because
            // Unity doesn't write defines for Release configuration, so we can't
            // rely on this)
            Version unityVersion = null;
            foreach (var project in solution.GetTopLevelProjects())
            {
                foreach (var configuration in project.ProjectProperties.GetActiveConfigurations<IManagedProjectConfiguration>())
                {
                    unityVersion = UnityProjectFileCacheProvider.GetVersionFromDefines(configuration.DefineConstants ?? string.Empty,
                        unityVersion);
                }
            }

            // If all else fails, default to 5.4. The majority of the tests will
            // rely on this being set.
            return unityVersion ?? new Version(5, 4);
        }
    }
}