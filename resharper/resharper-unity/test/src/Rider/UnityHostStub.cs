using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Rider
{
    [SolutionComponent]
    public class UnityHostStub : UnityHost
    {
        public UnityHostStub(ISolution solution) : base(solution, true)
        {
        }
    }
}