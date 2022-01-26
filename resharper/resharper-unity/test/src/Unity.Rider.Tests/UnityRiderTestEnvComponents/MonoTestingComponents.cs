using JetBrains.Application.Environment;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestEnvComponents
{
    // The Unix file system watcher appears to be much slower to initialise than Windows. It's enabled by default, and
    // disabled/re-enabled at the start and end of each test. But re-enabling reinitialises the watchers, which is very
    // slow on Mono - killing performance in tests. It also means we initialise the watchers at the end of a test only
    // to dispose at the start of the next test, which is completely unnecessary.
    [EnvironmentComponent]
    public class MonoFileSystemTrackerDisabler
    {
        public MonoFileSystemTrackerDisabler(IFileSystemTracker fileSystemTracker)
        {
            if (PlatformUtil.IsRunningOnMono)
                fileSystemTracker.Enabled = false;
        }
    }
}