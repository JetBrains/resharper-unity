using System;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityHost
    {
        private readonly bool myIsInTests;
        private readonly FrontendBackendModel myModel;

        public UnityHost(Lifetime lifetime, ISolution solution, IShellLocks shellLocks, DeferredCacheController deferredCacheController, bool isInTests = false)
        {
            myIsInTests = isInTests;
            if (myIsInTests)
                return;

            myModel = solution.GetProtocolSolution().GetFrontendBackendModel();
            deferredCacheController.CompletedOnce.Advise(lifetime, v =>
            {
                if (v)
                {
                    shellLocks.Tasks.StartNew(lifetime, Scheduling.MainDispatcher,
                        () => { myModel.IsDeferredCachesCompletedOnce.Value = true; });
                }
            });
        }

        public void PerformModelAction(Action<FrontendBackendModel> action)
        {
            if (myIsInTests)
                return;

            action(myModel);
        }

        public T GetValue<T>(Func<FrontendBackendModel, T> getter)
        {
            return getter(myModel);
        }
    }
}