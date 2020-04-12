using JetBrains.Application.FileSystemTracker;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ProjectModel
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