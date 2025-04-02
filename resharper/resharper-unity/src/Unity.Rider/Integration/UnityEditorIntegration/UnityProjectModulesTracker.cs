#nullable enable
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Components;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.Rider.Model.Unity.BackendUnity;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.UnityEditorIntegration;

[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public class UnityProjectModulesTracker : IUnityProjectLazyComponent
{
    private readonly BackendUnityHost myBackendUnityHost;
    public IViewableProperty<UnityPlatformInfo?> UnityPlatformInfo { get; }

    public UnityProjectModulesTracker(Lifetime lifetime, BackendUnityHost backendUnityHost)
    {
        myBackendUnityHost = backendUnityHost;
        UnityPlatformInfo = new ViewableProperty<UnityPlatformInfo?>(null);
        BindToProtocol(lifetime);
    }

    private void BindToProtocol(Lifetime lt)
    {
        myBackendUnityHost.BackendUnityModel
            .ViewNotNull(lt, (l, model) => model.UnityPlatformInfo.FlowInto(l, UnityPlatformInfo));
    }
}