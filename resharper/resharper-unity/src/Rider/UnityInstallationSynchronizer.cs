using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityInstallationSynchronizer: UnityReferencesTracker.IHandler
    {
        private readonly UnityHost myHost;
        private readonly UnityInstallationFinder myFinder;
        private readonly UnityVersion myUnityVersion;

        public UnityInstallationSynchronizer(UnityHost host, UnityInstallationFinder finder, UnityVersion unityVersion)
        {
            myHost = host;
            myFinder = finder;
            myUnityVersion = unityVersion;
        }
        
        public void OnReferenceAdded(IProject unityProject, Lifetime projectLifetime)
        {
        }

        public void OnSolutionLoaded(UnityProjectsCollection solution)
        {
            var version = myUnityVersion.GetActualVersionForSolution();
            var path = myFinder.GetApplicationPath(version);
            if (path == null)
                return;
            var contentPath = myFinder.GetApplicationContentsPath(version);

            myHost.PerformModelAction(rd =>
            {
                rd.ApplicationPath.SetValue(path.FullPath);
                rd.ApplicationContentsPath.SetValue(contentPath.FullPath);
            });
        }
    }
}