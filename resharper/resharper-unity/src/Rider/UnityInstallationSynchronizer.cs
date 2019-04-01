using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityInstallationSynchronizer
    {
        public UnityInstallationSynchronizer(Lifetime lifetime, UnitySolutionTracker solutionTracker,
                                             UnityHost host, UnityVersion unityVersion, UnityReferencesTracker referencesTracker)
        {
            solutionTracker.IsUnityProjectFolder.Advise(lifetime, res =>
            {
                if (!res) return;
                NotifyFrontend(host, unityVersion);
            });

            referencesTracker.HasUnityReference.Advise(lifetime, res =>
            {
                if (!res) return;
                NotifyFrontend(host, unityVersion);
            });
        }

        private static void NotifyFrontend(UnityHost host, UnityVersion unityVersion)
        {
            var version = unityVersion.GetActualVersionForSolution();
            var info = UnityInstallationFinder.GetApplicationInfo(version);
            if (info == null)
                return;

            host.PerformModelAction(rd =>
            {
                // ApplicationPath may be already set via UnityEditorProtocol, which is more accurate
                if (!rd.ApplicationPath.HasValue())
                    rd.ApplicationPath.SetValue(info.Path.FullPath);
                if (!rd.ApplicationContentsPath.HasValue())
                {
                    var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(version);
                    if (contentsPath != null)
                        rd.ApplicationContentsPath.SetValue(contentsPath.FullPath);
                }
                if (!rd.ApplicationVersion.HasValue() && info.Version != null)
                    rd.ApplicationVersion.SetValue(UnityVersion.VersionToString(info.Version));
            });
        }
    }
}