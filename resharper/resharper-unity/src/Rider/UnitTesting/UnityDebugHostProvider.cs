using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DebuggerFacade;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.UnitTestFramework;
using JetBrains.ReSharper.UnitTestFramework.Launch;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [UnitTestHostProvider]
    public class UnityDebugHostProvider : RiderDebugHostProvider
    {
        public UnityDebugHostProvider(IDebuggerFacade debuggerFacade, 
                                      ILogger logger)
            : base(debuggerFacade, logger)
        {
        }

        public override ITaskRunnerHostController CreateHostController(IUnitTestLaunch launch)
        {
            var innerHostController = base.CreateHostController(launch);
            return new UnityTaskRunnerHostController(innerHostController, 
                                                     launch.Solution.GetComponent<IShellLocks>(),
                                                     launch.Solution.GetComponent<IUnityController>(),
                                                     "Debug");
        }
    }
}