using JetBrains.Application.FileSystemTracker;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel.Caches;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [SolutionComponent]
    public class UnityVersionStub : Unity.UnityVersion
    {
        public UnityVersionStub(UnityProjectFileCacheProvider unityProjectFileCache, ISolution solution, IFileSystemTracker fileSystemTracker, Lifetime lifetime)
            : base(unityProjectFileCache, solution, fileSystemTracker, lifetime, true)
        {
        }
    }
}