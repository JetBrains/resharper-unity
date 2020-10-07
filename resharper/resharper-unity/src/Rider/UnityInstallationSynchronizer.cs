using System;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityInstallationSynchronizer
    {
        private readonly UnityEditorProtocol myUnityEditorProtocol;

        public UnityInstallationSynchronizer(Lifetime lifetime,
                                             UnityHost host, UnityVersion unityVersion,
                                                 UnityEditorProtocol unityEditorProtocol)
        {
            myUnityEditorProtocol = unityEditorProtocol;
            unityVersion.ActualVersionForSolution.Advise(lifetime, version => NotifyFrontend(host, unityVersion, version));
        }

        private void NotifyFrontend(UnityHost host, UnityVersion unityVersion, Version version)
        {
            host.PerformModelAction(rd =>
            {
                // if model is there, then ApplicationPath was already set via UnityEditorProtocol, it would be more correct than any counted value
                if (myUnityEditorProtocol.BackendUnityModel.Value != null)
                    return;

                var info = UnityInstallationFinder.GetApplicationInfo(version, unityVersion);
                if (info == null)
                    return;

                var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(info.Path);
                rd.UnityApplicationData.SetValue(new UnityApplicationData(info.Path.FullPath,
                    contentsPath.FullPath,
                    UnityVersion.VersionToString(info.Version),
                    UnityVersion.RequiresRiderPackage(info.Version)
                ));
            });
        }
    }
}