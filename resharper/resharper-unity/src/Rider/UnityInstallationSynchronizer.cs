using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Util;

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
            var applicationPath = unityVersion.GetActualAppPathForSolution();

            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX && !applicationPath.ExistsDirectory
                || PlatformUtil.RuntimePlatform != PlatformUtil.Platform.MacOsX && !applicationPath.ExistsFile)
            {
                var info = UnityInstallationFinder.GetApplicationInfo(version);
                if (info == null)
                    return;
                applicationPath = info.Path;
                version = info.Version;
            }

            host.PerformModelAction(rd =>
            {
                // ApplicationPath may be already set via UnityEditorProtocol, which will obviously be correct
                if (!rd.ApplicationPath.HasValue())
                    rd.ApplicationPath.SetValue(applicationPath.FullPath);

                if (!rd.ApplicationContentsPath.HasValue())
                {
                    var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(applicationPath);
                    if (!contentsPath.IsEmpty)
                        rd.ApplicationContentsPath.SetValue(contentsPath.FullPath);
                }
                if (!rd.ApplicationVersion.HasValue() && version != null)
                    rd.ApplicationVersion.SetValue(UnityVersion.VersionToString(version));
            });
        }
    }
}