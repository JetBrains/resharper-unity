using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.Rider.Model;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityInstallationSynchronizer
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;

        public UnityInstallationSynchronizer(Lifetime lifetime, UnitySolutionTracker solutionTracker,
                                             UnityHost host, UnityVersion unityVersion, UnityReferencesTracker referencesTracker,
                                                 UnityEditorProtocol unityEditorProtocol)
        {
            myUnityEditorProtocol = unityEditorProtocol;
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

        private void NotifyFrontend(UnityHost host, UnityVersion unityVersion)
        {
            host.PerformModelAction(rd =>
            {
                // if model is there, then ApplicationPath was already set via UnityEditorProtocol, it would be more correct than any counted value
                if (myUnityEditorProtocol.UnityModel.Value != null) 
                    return;

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
                
                var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(applicationPath);
                rd.UnityApplicationData.SetValue(new UnityApplicationData(applicationPath.FullPath, contentsPath.FullPath, UnityVersion.VersionToString(version)));
            });
        }
    }
}