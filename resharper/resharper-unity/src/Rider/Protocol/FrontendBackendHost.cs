using System;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Protocol
{
    /// <summary>
    /// This class simply exists to allow running tests without a frontend/backend connection. Any model action is ignored.
    /// </summary>
    [SolutionComponent]
    public class FrontendBackendHost
    {
        private readonly bool myIsInTests;

        // This will only ever be null when running tests. The value does not change for the lifetime of the solution.
        // Prefer using this field over calling GetFrontendBackendModel(), as that method will throw in tests
        [CanBeNull] public readonly FrontendBackendModel Model;

        public FrontendBackendHost(Lifetime lifetime, ISolution solution, IShellLocks shellLocks,
                                   DeferredCacheController deferredCacheController, bool isInTests = false)
        {
            myIsInTests = isInTests;
            if (myIsInTests)
                return;

            // This will throw in tests, as GetProtocolSolution will return null
            var model = solution.GetProtocolSolution().GetFrontendBackendModel();
            Model = model;

            // TODO: Where is the best place to advise frontend/backend model?
            deferredCacheController.CompletedOnce.Advise(lifetime, v =>
            {
                if (v)
                {
                    shellLocks.Tasks.StartNew(lifetime, Scheduling.MainDispatcher,
                        () => { model.IsDeferredCachesCompletedOnce.Value = true; });
                }
            });
        }

        public bool IsAvailable => !myIsInTests && Model != null;

        // Convenience method to fire and forget an action on the model (e.g. set a value, fire a signal, etc). Fire and
        // forget means it's safe to use during testing, when there won't be a frontend model available, and Model will
        // be null.
        // There is not a Do that takes in a Func to return a value, as that cannot be called reliably in tests. Use
        // Model directly in this case, check for null and do whatever is appropriate for the callsite.
        public void Do(Action<FrontendBackendModel> action)
        {
            if (myIsInTests)
                return;

            action(Model);
        }
    }
}