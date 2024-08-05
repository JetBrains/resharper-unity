using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework.Execution.Hosting;
using JetBrains.Rider.Backend.Features.Unity;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Core.Feature.UnitTesting
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class UnityTaskRunnerHostControllerExtension : RunUnityTaskRunnerHostControllerExtension
    {
        private static readonly IDictionary<string, string> ourAvailableProviders = new Dictionary<string, string>
        {
            { WellKnownHostProvidersIds.RunProviderId, "Run" },
            { WellKnownHostProvidersIds.DebugProviderId, "Debug" }
        };

        public UnityTaskRunnerHostControllerExtension(Lifetime lifetime, IUnityController unityController)
            : base(lifetime, unityController, ourAvailableProviders)
        {
        }
    }
}
