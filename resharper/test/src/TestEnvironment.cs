using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.platforms;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Host.Features.Platforms;
using JetBrains.ReSharper.Host.Features.Runtime;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

[assembly: RequiresSTA]

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ZoneDefinition]
    public interface IUnityTestZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>
    {
    }

    [SetUpFixture]
    public class TestEnvironment : ExtensionTestEnvironmentAssembly<IUnityTestZone>
    {
        static TestEnvironment()
        {
            SetupLogging();
            SetJetTestPackagesDir();
            try
            {
                RegisterAssemblyResolver();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void SetupLogging()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var url = new Uri(executingAssembly.CodeBase);
            var assemblyPath = FileSystemPath.Parse(url.AbsolutePath);
            var assemblyDir = assemblyPath.Directory;
            var configfile = assemblyDir.CombineWithShortName(assemblyPath.NameWithoutExtension + "_log.xml");

            // /resharper/build/tests.rider/bin/Debug/net461/JetBrains.ReSharper.Plugins.Unity.Tests.Rider.log
            var logfile = assemblyDir.CombineWithShortName(assemblyPath.NameWithoutExtension + ".log");
            logfile.DeleteFile();

            // Set to TRACE to get logging on basically everything (including component containers)
            // Set to VERBOSE for most useful logging, but beware perf impact
            File.WriteAllText(configfile.FullPath,
                $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<configuration>
  <appender name=""file"" class=""JetBrains.Util.Logging.FileLogEventListener"" pattern=""%d{{HH:mm:ss.fff}} |%l| %-30c{
                        1
                    }| %M%n"">
    <arg>{logfile.FullPath}</arg>
  </appender>
  <root level=""VERBOSE"">
    <appender-ref>file</appender-ref>
  </root>
</configuration>
");
            Environment.SetEnvironmentVariable("RESHARPER_LOG_CONF", configfile.FullPath);
        }

        // The repositoryPath setting in nuget.config points to the location of the test nuget packages that are
        // downloaded as part of running tests. We can't have a path that is both Windows and *NIX friendly, so we set
        // an environment variable. We'll default to /resharper/test/JetTestPackages, but you can set it externally to
        // override and share a single repo between all checkouts
        private static void SetJetTestPackagesDir()
        {
            // NuGet doesn't like a a fully qualified path, it always seems to be relative. We always seem to be in
            // /resharper/test/data, so let's make it relative to current dir
            if (Environment.GetEnvironmentVariable("JET_TEST_PACKAGES_DIR") == null)
            {
                Environment.SetEnvironmentVariable("JET_TEST_PACKAGES_DIR", "../JetTestPackages",
                    EnvironmentVariableTarget.Process);
            }
        }

        // Resolve PresentationFramework on mac
        private static void RegisterAssemblyResolver()
        {
            var testDataPath = TestUtil.GetTestDataPathBase(typeof(TestEnvironment).Assembly);
            // /resharper/test/data/../../../rider/build/riderRD-2018.3-SNAPSHOT/lib/ReSharperHost/macos-x64/mono/lib/mono/4.5
            var monoPath =
                testDataPath.Parent.Parent.Parent.Combine(
                    "rider/build/riderRD-2018.3-SNAPSHOT/lib/ReSharperHost/macos-x64/mono/lib/mono/4.5");
            var assemblyResolver = new AssemblyResolver(new[] {monoPath});
            assemblyResolver.Install(AppDomain.CurrentDomain);
        }

        public override void SetUp()
        {
            try
            {
                base.SetUp();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

#if RIDER
    // We need this because the existing Mono platform providers are part of the ReSharperHost zones, which aren't
    // enabled during tests. Should they be?
    // TODO: Create a version of this for ReSharper tests
    // The path providers come from ReSharperHost and aren't available to the ReSharper SDK
    [PlatformsProvider]
    public class MyMonoPlatformProvider : IPlatformsProvider
    {
        private readonly ILogger myLogger;

        public MyMonoPlatformProvider(ILogger logger)
        {
            myLogger = logger;
        }

        public IReadOnlyCollection<PlatformInfo> GetPlatformsForShell()
        {
            if (PlatformUtil.IsRunningUnderWindows)
                return EmptyList<PlatformInfo>.Collection;

            var monoPathProviders = new List<IMonoPathProvider>();
            monoPathProviders.Add(new EnvMonoPathProvider(myLogger));
            monoPathProviders.Add(new LinuxDefaultMonoPathProvider());
            monoPathProviders.Add(new MacOsDefaultMonoPathProvider());

            var detector = new MonoRuntimeDetector(monoPathProviders, myLogger);
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
    }
#endif
}

