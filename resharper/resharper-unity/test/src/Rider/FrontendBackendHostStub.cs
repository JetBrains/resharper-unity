using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Rider
{
    [SolutionComponent]
    public class FrontendBackendHostStub : FrontendBackendHost
    {
        public FrontendBackendHostStub(Lifetime lifetime, ISolution solution, IShellLocks shellLocks,
                                       DeferredCacheController deferredCacheController)
            : base(lifetime, solution, shellLocks, deferredCacheController, true)
        {
        }
    }
}