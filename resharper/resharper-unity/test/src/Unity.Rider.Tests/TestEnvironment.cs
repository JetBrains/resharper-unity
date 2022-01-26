using System;
using System.IO;
using System.Reflection;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.ReSharper.Plugins.Json;
using JetBrains.ReSharper.Plugins.Unity.HlslSupport;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.ReSharper.Plugins.Yaml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Rider.Backend.Env;
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
[assembly: TestDataPathBase("resharper-unity/test/data/Unity.Rider")]
#pragma warning restore 618



namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests
{
    // Activate the zones we require for shell/solution containers. This is normally handled by product specific zone
    // activators. But we don't have any product environment zones, so these activators aren't loaded, and we need to
    // activate pretty much everything we need.
    // We need to explicitly activate the language zones, since PsiFeatureTestZone activates leaf languages, rather
    // than IPsiLanguageZone (which would activate all other languages due to inheritance). But we can't activate HLSL
    // on Mono, as the managed C++ Cpp PSI doesn't work on Mono
    [ZoneDefinition]
    public interface IRiderUnityTestsEnvZone : ITestsEnvZone
    {
    }

    [ZoneDefinition]
    public interface IRiderUnityTestsZone : IZone, IRequire<IRiderUnityPluginZone>, IRequire<PsiFeatureTestSlimZone>, IRequire<IPsiLanguageZone>
    {
        
    }
    
    
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
                HackTestDataInNugets.ApplyPatches();

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
        private static void ConfigureLoggingFolderPath()
        {
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

            // Set to TRACE to get logging on basically everything (including component containers)
            // Set to VERBOSE for most useful logging, but beware perf impact
            File.WriteAllText(configFile.FullPath,
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