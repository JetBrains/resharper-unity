using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityHost
    {
        public RdUnityModel Model { get; }

        public UnityHost(ISolution solution)
        {
            Model = solution.GetProtocolSolution().GetRdUnityModel();
        }
    }
}