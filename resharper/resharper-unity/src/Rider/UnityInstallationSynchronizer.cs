using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityInstallationSynchronizer
    {
        public UnityInstallationSynchronizer(Lifetime lifetime, UnitySolutionTracker solutionTracker,
                                             UnityHost host, UnityInstallationFinder finder, UnityVersion unityVersion)
        {
            solutionTracker.IsUnityProjectFolder.Advise(lifetime, isUnityProjectFolder =>
            {
                if (!isUnityProjectFolder) return;
                var version = unityVersion.GetActualVersionForSolution();
                var path = finder.GetApplicationPath(version);
                if (path == null)
                    return;

                var contentPath = finder.GetApplicationContentsPath(version);

                host.PerformModelAction(rd =>
                {
                    // ApplicationPath may be already set via UnityEditorProtocol, which is more accurate
                    if (!rd.ApplicationPath.HasValue())
                        rd.ApplicationPath.SetValue(path.FullPath);
                    if (!rd.ApplicationContentsPath.HasValue())
                        rd.ApplicationContentsPath.SetValue(contentPath.FullPath);
                    if (!rd.ApplicationVersion.HasValue())
                        rd.ApplicationVersion.SetValue(UnityVersion.VersionToString(version));
                });
            });
        }
    }
}