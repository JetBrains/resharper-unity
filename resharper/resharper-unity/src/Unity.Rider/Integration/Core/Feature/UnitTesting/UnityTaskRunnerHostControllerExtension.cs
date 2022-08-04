using System.Collections.Generic;
using JetBrains.Application.Threading;
using JetBrains.Application.UI.Controls;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework.Execution.Hosting;
using JetBrains.ReSharper.UnitTestFramework.Execution.Launch;
using JetBrains.Rider.Backend.Features.Unity;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [SolutionComponent]
    public class UnityTaskRunnerHostControllerExtension : RunUnityTaskRunnerHostControllerExtension
    {
        private readonly IDictionary<string, string> myAvailableProviders;
        
        public UnityTaskRunnerHostControllerExtension(Lifetime lifetime,
                                                      IShellLocks threading,
                                                      IUnityController unityController,
                                                      IBackgroundProgressIndicatorManager indicatorManager) 
            : base(lifetime, unityController, indicatorManager, threading)
        {
            myAvailableProviders = new Dictionary<string, string>
            {
                { WellKnownHostProvidersIds.RunProviderId, "Run" },
                { WellKnownHostProvidersIds.DebugProviderId, "Debug" }
            };
        }

        protected override string PluginName => "Unity plugin";
        
        protected override string GetProviderPresentableName(string hostId) => myAvailableProviders[hostId];
        
        public override bool IsApplicable(IUnitTestRun run) 
            => base.IsApplicable(run) && myAvailableProviders.ContainsKey(run.HostController.HostId);
    }
}