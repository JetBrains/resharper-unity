using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class RiderUnityUsagesNotification : UnityUsagesDeferredCachesNotification
    {
        private readonly UnityHost myUnityHost;

        public RiderUnityUsagesNotification(UnityHost unityHost, DeferredCacheController controller)
            : base(controller)
        {
            myUnityHost = unityHost;
        }

        protected override void ShowNotification()
        {
            myUnityHost.PerformModelAction(t => t.ShowDeferredCachesProgressNotification());
        }
    }
}