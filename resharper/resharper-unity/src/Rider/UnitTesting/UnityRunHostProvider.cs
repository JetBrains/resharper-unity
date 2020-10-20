using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [UnitTestHostProvider]
    public class UnityRunHostProvider : RunHostProvider
    {
        public override ITaskRunnerHostController CreateHostController(IUnitTestLaunch launch)
        {
            var innerHostController = base.CreateHostController(launch);
            return new UnityTaskRunnerHostController(innerHostController,
                                                     launch.Solution.GetComponent<IShellLocks>(),
                                                     launch.Solution.GetComponent<IUnityController>(),
                                                     "Run");
        }
    }
}