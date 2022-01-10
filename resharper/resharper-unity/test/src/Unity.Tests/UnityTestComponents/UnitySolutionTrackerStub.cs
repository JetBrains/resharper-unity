using JetBrains.Application.FileSystemTracker;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Tests.UnityTestComponents
{
    [SolutionComponent]
    public class UnitySolutionTrackerStub : UnitySolutionTracker
    {
        public UnitySolutionTrackerStub(ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime)
            : base(solution, fileSystemTracker, lifetime, true)
        {
        }
    }
}