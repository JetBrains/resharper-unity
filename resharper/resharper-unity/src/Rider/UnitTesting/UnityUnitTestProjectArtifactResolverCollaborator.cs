using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.UnitTestFramework.Exploration;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.UnitTesting
{
    [SolutionComponent]
    public class UnityUnitTestProjectArtifactResolverCollaborator : IUnitTestProjectArtifactResolverCollaborator
    {
        private readonly UnitySolutionTracker myUnitySolutionTracker;

        public UnityUnitTestProjectArtifactResolverCollaborator(UnitySolutionTracker unitySolutionTracker)
        {
            myUnitySolutionTracker = unitySolutionTracker;
        }
        
        public bool CanResolveArtifact(IProject project, TargetFrameworkId targetFrameworkId)
        {
            return myUnitySolutionTracker.IsUnityProject.Maybe.Value;
        }

        public FileSystemPath ResolveArtifact(IProject project, TargetFrameworkId targetFrameworkId)
        {
            return project.Location.Combine("Library").Combine("ScriptAssemblies");
        }
    }
}