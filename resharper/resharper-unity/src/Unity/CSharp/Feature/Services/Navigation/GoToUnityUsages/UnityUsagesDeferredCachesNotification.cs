using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DeferredCaches;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
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