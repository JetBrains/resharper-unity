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
        private readonly UnityNUnitServiceProvider myServiceProvider;

        public UnityUnitTestProjectArtifactResolverCollaborator(UnitySolutionTracker unitySolutionTracker, UnityNUnitServiceProvider serviceProvider)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myServiceProvider = serviceProvider;
        }
        
        public bool CanResolveArtifact(IProject project, TargetFrameworkId targetFrameworkId)
        {
            return myUnitySolutionTracker.IsUnityGeneratedProject.Maybe.Value && myServiceProvider.IsUnityUnitTestStrategy();
        }

        public FileSystemPath ResolveArtifact(IProject project, TargetFrameworkId targetFrameworkId)
        {
            var dllName = project.GetOutputFilePath(targetFrameworkId).Name;
            return project.Location.Combine("Library").Combine("ScriptAssemblies").Combine(dllName).ToNativeFileSystemPath();
        }
    }
}