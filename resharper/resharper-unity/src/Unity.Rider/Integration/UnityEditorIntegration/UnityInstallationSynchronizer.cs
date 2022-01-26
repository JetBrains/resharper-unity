using System;
using System.Threading.Tasks;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Base;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.Rider.Model.Unity;
using JetBrains.Threading;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration
{
    [SolutionComponent]
    public class UnityInstallationSynchronizer
    {
        private readonly Lifetime myLifetime;
        private readonly ISolution mySolution;
        private readonly BackendUnityHost myBackendUnityHost;

        public UnityInstallationSynchronizer(Lifetime lifetime,
            ISolution solution,
                                             FrontendBackendHost frontendBackendHost,
                                             BackendUnityHost backendUnityHost,
                                             UnityVersion unityVersion)
        {
            myLifetime = lifetime;
            mySolution = solution;
            myBackendUnityHost = backendUnityHost;
            unityVersion.ActualVersionForSolution.Advise(lifetime,
                version => NotifyFrontend(frontendBackendHost, unityVersion, version).NoAwait());
        }

        private async Task NotifyFrontend(FrontendBackendHost host, UnityVersion unityVersion, Version version)
        {
            if (version == new Version(0,0))
                return;

            var info = await mySolution.Locks.Tasks.StartNew(myLifetime, Scheduling.FreeThreaded,
                () => UnityInstallationFinder.GetApplicationInfo(version, unityVersion));

            host.Do(rd =>
            {
                // if model is there, then ApplicationPath was already set via UnityEditorProtocol, it would be more
                // correct than any counted value
                if (myBackendUnityHost.BackendUnityModel.Value != null)
                    return;

                if (info == null)
                    return;

                var contentsPath = UnityInstallationFinder.GetApplicationContentsPath(info.Path);
                rd.UnityApplicationData.SetValue(new UnityApplicationData(info.Path.FullPath,
                    contentsPath.FullPath,
                    UnityVersion.VersionToString(info.Version),
                    null, null, null));
                rd.RequiresRiderPackage.Set(UnityVersion.RequiresRiderPackage(info.Version));
            });
        }
    }
}