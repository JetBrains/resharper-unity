using System.Collections.Generic;
using JetBrains.Application.Environment;
using JetBrains.Application.FileSystemTracker;
using JetBrains.Application.platforms;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Host.Features.Platforms;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
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

    // The existing Mono platform providers are part of the ReSharperHost zones, which aren't enabled during tests
    [PlatformsProvider]
    public class TestMonoPlatformProvider : IPlatformsProvider
    {
        public IReadOnlyCollection<PlatformInfo> GetPlatformsForShell()
        {
            if (PlatformUtil.IsRunningUnderWindows)
                return EmptyList<PlatformInfo>.Collection;

            var monoPathProviders = new List<IMonoPathProvider>();
            monoPathProviders.Add(new EnvMonoPathProvider());
            monoPathProviders.Add(new LinuxDefaultMonoPathProvider());
            monoPathProviders.Add(new MacOsDefaultMonoPathProvider());

            var detector = new MonoRuntimeDetector(monoPathProviders);
            var monoRuntimes = detector.DetectMonoRuntimes();

            return MonoPlatformsProvider.GetPlatforms(monoRuntimes[0]);
        }

        public IReadOnlyCollection<PlatformInfo> GetPlatformsForSolution()
        {
            return EmptyList<PlatformInfo>.Collection;
        }

        public TargetFrameworkId DetectPlatformIdByReferences(AssemblyNameInfo corlibReference,
            IReadOnlyCollection<AssemblyNameInfo> otherReferences,
            IReadOnlyCollection<PlatformInfo> platforms)
        {
            return null;
        }

        public IReadOnlyDictionary<IAdvancedGuessMatcher, TargetFrameworkId> GetAdvancedPlatformMatchers(
            IReadOnlyCollection<PlatformInfo> platforms)
        {
            return EmptyDictionary<IAdvancedGuessMatcher, TargetFrameworkId>.Instance;
        }

        public int Priority => 200;
    }
}