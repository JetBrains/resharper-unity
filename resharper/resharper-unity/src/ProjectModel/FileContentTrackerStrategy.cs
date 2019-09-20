using System;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost.LiveTracking;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.ProjectModel
{
    [SolutionInstanceComponent]
    public class UnityFileContentTrackerStrategy : IFileContentTrackerStrategy
    {
        private readonly UnitySolutionTracker mySolutionTracker;

        public UnityFileContentTrackerStrategy(UnitySolutionTracker solutionTracker)
        {
            mySolutionTracker = solutionTracker;
        }
        
        public bool IsApplicable(FileSystemPath path)
        {
            return mySolutionTracker.IsUnityGeneratedProject.Value 
                   
                   && (path.ExtensionNoDot == "csproj" || path.ExtensionNoDot == "sln");
        }

        public long GetCurrentStamp(FileSystemPath path)
        {
            return path.ReadAllText2().Text.GetHashCode();
        }

        public int Priority => 10;
    }
}