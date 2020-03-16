using System;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Psi.Caches;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityHost
    {
        private readonly bool myIsInTests;
        private readonly RdUnityModel myModel;

        public UnityHost(Lifetime lifetime, ISolution solution, IShellLocks shellLocks, DeferredCacheController deferredCacheController, bool isInTests = false)
        {
            myIsInTests = isInTests;
            if (myIsInTests)
                return;

            myModel = solution.GetProtocolSolution().GetRdUnityModel();
            deferredCacheController.CompletedOnce.Advise(lifetime, v =>
            {
                if (v)
                {
                    shellLocks.Tasks.StartNew(lifetime, Scheduling.MainDispatcher,
                        () => { myModel.IsDeferredCachesCompletedOnce.Value = true; });
                }
            });
        }

        public void PerformModelAction(Action<RdUnityModel> action)
        {
            if (myIsInTests)
                return;

            action(myModel);
        }

        public T GetValue<T>(Func<RdUnityModel, T> getter)
        {
            return getter(myModel);
        }
    }
}