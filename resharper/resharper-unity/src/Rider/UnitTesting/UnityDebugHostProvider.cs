using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.DebuggerFacade;
using JetBrains.ReSharper.Host.Features.UnitTesting;
using JetBrains.ReSharper.Host.Features.Unity;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
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
            if (!launch.Solution.GetComponent<UnitySolutionTracker>().IsUnityProject.HasTrueValue())
                return innerHostController;
            
            return new UnityTaskRunnerHostController(innerHostController, 
                                                     launch.Solution.GetComponent<IShellLocks>(),
                                                     launch.Solution.GetComponent<IUnityController>(),
                                                     "Debug");
        }
    }
}