using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class UnityHost
    {
        public RdUnityModel Model { get; }

        public UnityHost(ISolution solution, IShellLocks locks)
        {
            // TODO: this shouldn't be up in tests until we figure out how to test unity-editor requiring features
            if (locks.Dispatcher.IsAsyncBehaviorProhibited)
                return;
            
            Model = solution.GetProtocolSolution().GetRdUnityModel();
        }
    }
}