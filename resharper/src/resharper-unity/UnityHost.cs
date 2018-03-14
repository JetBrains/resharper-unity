using JetBrains.ProjectModel;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity
{
    [SolutionComponent]
    public class UnityHost
    {
        public RdUnityModel Model { get; }

        public UnityHost(RdUnityModel model)
        {
            Model = model;
        }
    }
}