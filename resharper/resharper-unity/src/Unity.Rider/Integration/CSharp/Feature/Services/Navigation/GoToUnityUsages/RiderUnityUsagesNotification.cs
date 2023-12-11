using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [SolutionComponent]
    public class RiderUnityUsagesNotification(FrontendBackendHost frontendBackendHost, DeferredCacheController controller, Lifetime lifetime, IShellLocks locks) : UnityUsagesDeferredCachesNotification(controller)
    {
        protected override void ShowNotification()
        {
            frontendBackendHost.Do(t => locks.ExecuteOrQueue(lifetime, "RiderUnityUsagesNotification.ShowDeferredCachesProgressNotification", t.ShowDeferredCachesProgressNotification));
        }
    }
}