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
            var path = unityVersion.GetActualAppPathForSolution();
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.MacOsX && !path.ExistsDirectory
                || PlatformUtil.RuntimePlatform != PlatformUtil.Platform.MacOsX && !path.ExistsFile)
            {
                var info = UnityInstallationFinder.GetApplicationInfo(version);
                if (info == null)
                    return;
                path = info.Path;
                version = info.Version;
            }
            
            host.PerformModelAction(rd =>
            {
                // ApplicationPath may be already set via UnityEditorProtocol, which is more accurate
                if (!rd.ApplicationPath.HasValue())
                    rd.ApplicationPath.SetValue(path.FullPath);
                if (!rd.ApplicationContentsPath.HasValue())
                {
                    var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(path);
                    if (contentsPath != null)
                        rd.ApplicationContentsPath.SetValue(contentsPath.FullPath);
                }
                if (!rd.ApplicationVersion.HasValue() && version != null)
                    rd.ApplicationVersion.SetValue(UnityVersion.VersionToString(version));
            });
        }
    }
}