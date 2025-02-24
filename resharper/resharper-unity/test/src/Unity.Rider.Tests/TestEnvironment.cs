using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.ReSharper.Features.ReSpeller;
using JetBrains.ReSharper.FeaturesTestFramework.SpellEngineStub;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using JetBrains.Util.Logging;
using NUnit.Framework;

[assembly: RequiresThread(System.Threading.ApartmentState.STA)]

// This attribute is marked obsolete but is still supported. Use is discouraged in preference to convention, but the
// convention doesn't work for us. That convention is to walk up the tree from the executing assembly and look for a
// relative path called "test/data". This doesn't work because our common "build" folder is one level above our
// "test/data" folder, so it doesn't get found. We want to keep the common "build" folder, but allow multiple "modules"
// with separate "test/data" folders. E.g. "resharper-unity" and "resharper-yaml"
#pragma warning disable 618
#if INDEPENDENT_BUILD
[assembly: TestDataPathBase("resharper-unity/test/data/Unity.Rider")]
#else
[assembly: TestDataPathBase("Plugins/ReSharperUnity/resharper/resharper-unity/test/data/Unity.Rider")]
#endif
#pragma warning restore 618

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests
{
    // Encapsulates the set of requirements for the host/environment zone (not to be confused with the environment
    // container). It is the root product zone that is used to bootstrap and activate the other zones. It is
    // automatically activated by ExtensionTestEnvironmentAssembly and is used to mark and therefore include the zone
    // activator for the required product zones.
    // This should be used only for environment components, as it is one of the only zones active during environment
    // container composition.
    [ZoneDefinition]
    public interface IRiderUnityTestsEnvZone : ITestsEnvZone
    {
    }

    // Encapsulates the set of required product zones needed to run the tests. PsiFeatureTestZone handles most of this,
    // adding requirements for zones such as DaemonZone, NavigationZone and ICodeEditingZone, as well as the majority of
    // bundled languages. This zone should require or inherit from any custom plugin zones, and explicitly require
    // custom languages (PsiFeaturesTestZone does not require IPsiLanguageZone, which would activate all languages via
    // inheritance).
    // Use this zone for all custom or overriding components in the tests.
    [ZoneDefinition]
    public interface IRiderUnityTestsZone : IZone,
        IRequire<PsiFeatureTestZone>,
        IRequire<IRiderUnityPluginZone>,
        IRequire<IUnityShaderZone>,
        IRequire<IUnityPluginZone>,
        IRequire<IReSpellerZone>,
        IRequire<IReSpellerTestStubZone>
    {
    }

    // Activates the product zones required for tests. It is an environment component, and invoked while the environment
    // container is being composed. As such, it must be in a zone that is already active - i.e. a host environment zone,
    // such as the test env zone. (A completely missing zone marker will also include it in the environment container.)
    // It activates the tests zone, which is the set of all zones required to run the tests, including both production
    // components and test components, and will be used to filter the shell and solution containers.

    // Note that not all Rider components can be tested, as many of them require the protocol. It appears that we can't
    // activate IResharperHost* zones
    [ZoneActivator]
    [ZoneMarker(typeof(IRiderUnityTestsEnvZone))]
    public class UnityTestZonesActivator : IActivate<IRiderUnityTestsZone>
    {
    }

    [SetUpFixture]
    public class TestEnvironment : ExtensionTestEnvironmentAssembly<IRiderUnityTestsEnvZone>
    {
        static TestEnvironment()
        {
            try
            {
                ConfigureLoggingFolderPath();
                // ConfigureStartupLogging();
                SetJetTestPackagesDir();

                if (PlatformUtil.IsRunningOnMono)
                {
                    // Temp workaround for GacCacheController, which adds all Mono GAC paths into a dictionary without
                    // checking for duplicates
                    Environment.SetEnvironmentVariable("MONO_GAC_PREFIX", "/foo");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // The default logger outputs to $TMPDIR/JetLogs/ReSharperTests/resharper.log, which is not very helpful when
        // we're testing more than one assembly. This sets up an environment variable to output the log to
        // /resharper/build/{project}/logs
        [Conditional("INDEPENDENT_BUILD")]
        private static void ConfigureLoggingFolderPath()
        {
            if (Environment.GetEnvironmentVariable(Logger.JETLOGS_DIRECTORY_ENV_VARIABLE) == null)
                Environment.SetEnvironmentVariable(Logger.JETLOGS_DIRECTORY_ENV_VARIABLE, GetLogsFolder().FullPath);
        }

        private static FileSystemPath GetLogsFolder()
        {
            // /resharper/build/{project}/bin/Debug/net461
            var assemblyPath = FileSystemPath.Parse(Assembly.GetExecutingAssembly().Location).Directory;
            var buildRoot = assemblyPath.Parent.Parent.Parent;

            // /resharper/build/{project}/logs
            var logsPath = buildRoot.Combine("logs");
            if (!logsPath.ExistsDirectory)
                logsPath.CreateDirectory();
            return logsPath;
        }

        // BaseTestNoShell.SetUp will set up all the logging we normally need. It sets up per-test and common logs to
        // write at VERBOSE level to files in $TMPDIR/JetLogs/ReSharperTests.
        // However, it only starts logging once configured, which is when SetUp is called before a test starts. Use this
        // method to log during startup, to diagnose any kind of startup issues (e.g. running tests on a Mac). This is
        // not normally useful, and can have a significant performance impact - a ~35 second run dropped to ~29 seconds
        // on my MacBook. This is mostly due to adding a third VERBOSE logging file appender, lots of VERBOSE logging
        // during startup, and the cost of creating FileLogEventListener from config (getting current process ID to
        // substitute into the file path pattern can be surprisingly expensive on my Mac when called for each test).
        // Note that TRACE logging can be enabled per-test by overriding BaseTestNoShell.TestCategories
        private static void ConfigureStartupLogging()
        {
            var logsPath = GetLogsFolder();
            var configFile = logsPath.Combine("resharper_startup_conf.xml");
            var logfile = logsPath.Combine("resharper_startup.log");
            if (logfile.ExistsFile)
                logfile.DeleteFile();

            // Set to VERBOSE to get logging on basically everything (including component containers)
            // Set to TRACE to get more logging, but beware of perf impact
            // lang=xml
            var contents =
                $@"<?xml version=""1.0"" encoding=""UTF-8"" ?>
                   <configuration>
                     <appender name=""file"" class=""JetBrains.Util.Logging.FileLogEventListener"" pattern=""%d{{HH:mm:ss.fff}} |%l| %-30c{{1}}| %M%n"">
                       <arg>{logfile.FullPath}</arg>
                     </appender>
                     <root level=""VERBOSE"">
                       <appender-ref>file</appender-ref>
                     </root>

                     <!-- Useful trace categories for zone details. (Uncomment, but don't commit!) -->

                     <!-- Trace all known components in EnvironmentPartCatalogSet (lots of output!) -->
                     <!-- Trace components which were in FullPartCatalogSet but did not make it into EnvironmentPartCatalogSet -->
                     <!-- <logger name=""JetBrains.Application.Environment.JetEnvironment"" level=""TRACE"">
                       <appender-ref>file</appender-ref>
                     </logger> -->
                     <!-- Trace eligible component sets for CatalogComponentSource (lots of output!) -->
                     <!-- <logger name=""JetBrains.Application.Extensibility.CatalogComponentSource"" level=""TRACE"">
                       <appender-ref>file</appender-ref>
                     </logger> -->
                     <!-- To see negative zones by propagation -->
                     <!-- <logger name=""JetBrains.Application.Environment.RunsProducts"" level=""TRACE"">
                       <appender-ref>file</appender-ref>
                     </logger> -->
                     <!-- Trace out the part catalog zone mapping - what zones are mapped to which namespaces and types -->
                     <!-- <logger name=""JetBrains.Application.BuildScript.Application.Catalogs.PartCatalogZoneMapping"" level=""TRACE"">
                       <appender-ref>file</appender-ref>
                     </logger> -->
                   </configuration>";
            File.WriteAllText(configFile.FullPath, contents);
            Environment.SetEnvironmentVariable("RESHARPER_LOG_CONF", configFile.FullPath);
        }

        // The repositoryPath setting in nuget.config points to the location of the test nuget packages that are
        // downloaded as part of running tests. We can't have a path that is both Windows and *NIX friendly, so we set
        // an environment variable.
        // If the ~/JetTestPackages folder exists, we'll use that. This is recommended, as it means packages are shared
        // across different plugin instances. If the folder doesn't exist, we put it next to the TestDataPath, e.g.
        // test/data/../JetTestPackages.
        // Note that repositoryPath can be a relative path, and it's resolved against the location of the nuget.config
        // file it's set in - remember that nuget.config files are discovered and resolved hierarchically.
        private static void SetJetTestPackagesDir()
        {
            if (Environment.GetEnvironmentVariable("JET_TEST_PACKAGES_DIR") == null)
            {
                var packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "JetTestPackages");
                if (!Directory.Exists(packages))
                {
                    TestUtil.SetHomeDir(typeof(TestEnvironment).Assembly);
                    var testData = TestUtil.GetTestDataPathBase(typeof(TestEnvironment).Assembly);
                    packages = testData.Parent.Combine("JetTestPackages").FullPath;
                }

                Environment.SetEnvironmentVariable("JET_TEST_PACKAGES_DIR", packages,
                    EnvironmentVariableTarget.Process);
            }
        }
    }
}
