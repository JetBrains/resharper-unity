using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.Parts;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents
{
    // The package refresh timer does not tick in the test shell. Tests call RefreshPackagesSynchronously instead.
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class TestPackageManager : PackageManager
    {
        public TestPackageManager(Lifetime lifetime, ISolution solution, ILogger logger,
                                  UnitySolutionTracker unitySolutionTracker,
                                  UnityVersion unityVersion,
                                  IFileSystemTracker fileSystemTracker,
                                  UnityPackageProjectResolution unityPackageProjectResolution)
            : base(lifetime, solution, logger, unitySolutionTracker, unityVersion, fileSystemTracker,
                unityPackageProjectResolution)
        {
        }

        public void RefreshPackagesSynchronously() => DoRefresh();
    }
}
