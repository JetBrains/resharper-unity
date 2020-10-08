using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.Application.License2;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using NUnit.Framework;

#if RIDER
using JetBrains.ReSharper.Host.Env;
#endif

[assembly: RequiresThread(System.Threading.ApartmentState.STA)]

// This attribute is marked obsolete but is still supported. Use is discouraged in preference to convention, but the
// convention doesn't work for us. That convention is to walk up the tree from the executing assembly and look for a
// relative path called "test/data". This doesn't work because our common "build" folder is one level above our
// "test/data" folder, so it doesn't get found. We want to keep the common "build" folder, but allow multiple "modules"
// with separate "test/data" folders. E.g. "resharper-unity" and "resharper-yaml"
#pragma warning disable 618
[assembly: TestDataPathBase("resharper-unity/test/data")]
#pragma warning restore 618

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    [ZoneDefinition]
    public interface IUnityTestZone : ITestsEnvZone

    {
    }

    [ZoneActivator]
    class CppTestZoneActivator : IActivate<ILanguageCppZone>, IActivate<PsiFeatureTestZone>
#if RIDER
        , IActivate<IRiderPlatformZone>
#endif
    ,IRequire<IUnityTestZone>
    {
        public bool ActivatorEnabled()
        {
            return true;
        }
    }

    [SetUpFixture]
    public class TestEnvironment : ExtensionTestEnvironmentAssembly<IUnityTestZone>
    {
        static TestEnvironment()
        {
            try
            {
                // SetupLogging();
                SetJetTestPackagesDir();
                HackTestDataInNugets.ApplyPatches();

                // Temp workaround for GacCacheController, which adds all Mono GAC paths into a dictionary without
                // checking for duplicates
                if (PlatformUtil.IsRunningOnMono)
                    Environment.SetEnvironmentVariable("MONO_GAC_PREFIX", "/foo");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // Default logging is verbose, but this is useful for debugging startup, before the tests get a chance to
        // initialise properly
        private static void SetupLogging()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var url = new Uri(executingAssembly.CodeBase);
            var assemblyPath = FileSystemPath.Parse(url.AbsolutePath);
            var assemblyDir = assemblyPath.Directory;
            var configFile = assemblyDir.CombineWithShortName(assemblyPath.NameWithoutExtension + "_log.xml");

            // /resharper/build/tests.rider/bin/Debug/net461/JetBrains.ReSharper.Plugins.Unity.Tests.Rider_log.log
            // Note that the logger will delete all files with basename.*, e.g. Rider.dll !!!!??!?!
            var logfile = assemblyDir.CombineWithShortName(assemblyPath.NameWithoutExtension + "_log.log");
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
