using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [SolutionComponent]
    public class UnityUsagesDeferredCachesNotification
    {
        private readonly DeferredCacheController myController;

        public UnityUsagesDeferredCachesNotification(DeferredCacheController controller)
        {
            myController = controller;
        }
        
        public void CheckAndShowNotification()
        {
            if (!myController.CompletedOnce.Value)
                ShowNotification();
        }

        protected virtual void ShowNotification()
        {
            
        }
    }
}