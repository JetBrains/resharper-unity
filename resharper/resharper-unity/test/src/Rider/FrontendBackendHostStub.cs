using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Feature.Caches;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Rider
{
    [SolutionComponent]
    public class FrontendBackendHostStub : FrontendBackendHost
    {
        public FrontendBackendHostStub(Lifetime lifetime, ISolution solution, IShellLocks shellLocks,
                                       PackageManager packageManager,
                                       DeferredCacheController deferredCacheController)
            : base(lifetime, solution, shellLocks, packageManager, deferredCacheController, true)
        {
        }
    }
}