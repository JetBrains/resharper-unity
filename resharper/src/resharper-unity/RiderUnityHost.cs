using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class RiderUnityHost
    {
        public RdUnityModel Model { get; }

        public RiderUnityHost(ISolution solution)
        {
            Model = solution.GetProtocolSolution().GetRdUnityModel();
        }
    }
}