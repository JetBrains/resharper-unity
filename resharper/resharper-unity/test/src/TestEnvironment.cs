using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using HarmonyLib;
using JetBrains.Application;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Application;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.HostParameters;
using JetBrains.Build.Serialization;
using JetBrains.Extension;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Storage;
using JetBrains.Util.Storage.Packages;
using JetBrains.Util.Storage.StructuredStorage;
using NuGet;
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
    public interface IUnityTestZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>
#if RIDER
        , IRequire<IRiderPlatformZone>
#endif
    {
    }

    [SetUpFixture]
    public class TestEnvironment : HackedExtensionTestEnvironmentAssembly<IUnityTestZone>
    {
        static TestEnvironment()
        {
            try
            {
                // SetupLogging();
                SetJetTestPackagesDir();
                HackTestDataInNugets.ApplyPatches();
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
        // If the ~/JetTestPackages folder exists, we'll use that. If not, put it next to the TestDataPath, e.g.
        // test/data/../JetTestPackages
        private static void SetJetTestPackagesDir()
        {
            // TODO: Temporary hack to expand environment variables BEFORE converting to a fully qualified path
            // Without this, all environment variables would have to be relative
            var harmony = new Harmony("com.jetbrains.resharper.tests::repositoryPath");
            var originalMethod = AccessTools.Method(typeof(JetNuGetSettingsV2), "ElementToValue");
            var prefixMethod = new HarmonyMethod(AccessTools.Method(typeof(TestEnvironment),
                nameof(New_JetNuGetSettingsV2_ElementToValue)));
            harmony.Patch(originalMethod, prefixMethod);

            if (Environment.GetEnvironmentVariable("JET_TEST_PACKAGES_DIR") == null)
            {
                // If path is relative, it's relative to the folder of the NuGet.config file
                var packages = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "JetTestPackages");
                if (!Directory.Exists(packages))
                {
                    var testData = TestUtil.GetTestDataPathBase(typeof(TestEnvironment).Assembly);
                    packages = testData.Parent.Combine("JetTestPackages").FullPath;
                }

                Environment.SetEnvironmentVariable("JET_TEST_PACKAGES_DIR", packages,
                    EnvironmentVariableTarget.Process);
            }
        }

        // The original method expands environment variables AFTER checking to see if it's a full or relative path
        private static bool New_JetNuGetSettingsV2_ElementToValue(JetNuGetSettingsV2 __instance, XElement element, bool isPath, IFileSystem ____fileSystem, ref string __result)
        {
            __result = null;
            if (element == null)
                return false;
            var optionalAttributeValue = Environment.ExpandEnvironmentVariables(element.GetOptionalAttributeValue("value"));
            if (!isPath || string.IsNullOrEmpty(optionalAttributeValue))
                __result = optionalAttributeValue;
            else
            {
                var configDirectory = Path.GetDirectoryName(__instance.ConfigFilePath);
                var pathRoot = Path.GetPathRoot(optionalAttributeValue);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse - GetPathRoot can return null on Mono
                var resolvedPath = pathRoot != null && pathRoot.Length == 1 && (pathRoot[0] == Path.DirectorySeparatorChar || optionalAttributeValue[0] == Path.AltDirectorySeparatorChar)
                        ? Path.Combine(Path.GetPathRoot(configDirectory), optionalAttributeValue.Substring(1)) : Path.Combine(configDirectory, optionalAttributeValue);
                __result = ____fileSystem.GetFullPath(resolvedPath);
            }
            return false;
        }
    }


    public abstract class HackedExtensionTestEnvironmentAssembly<TTestEnvironmentZone> : TestEnvironmentAssembly<TTestEnvironmentZone>
        where TTestEnvironmentZone : ITestsEnvZone
    {
        public override void SetUp()
        {
            var mainAssembly = GetType().Assembly;
            var productBinariesDir = mainAssembly.GetPath().Parent;
            var assemblyNameInfo = AssemblyNameInfo.Parse(mainAssembly.FullName);

            var packageArtifactDummy = new ApplicationPackageArtifact(new SubplatformName(assemblyNameInfo.Name),
                new JetSemanticVersion(assemblyNameInfo.Version), "FakeCompanyName", "FakeCompanyName", DateTime.UtcNow,
                null, null, EmptyList<ApplicationPackageFile>.InstanceList,
                EmptyList<ApplicationPackageReference>.InstanceList);
            var metafile = productBinariesDir /
                           NugetApplicationPackageConvention.GetJetMetadataEffectivePath(packageArtifactDummy);
            metafile.DeleteWithMoveAside();

            var packages =
                AllAssembliesLocator.GetAllAssembliesOnLocallyInstalledBinariesFlat(
                    new ProductBinariesDirArtifact(productBinariesDir));

            var packageFiles = new HashSet<ApplicationPackageFile>(
                EqualityComparer.Create<ApplicationPackageFile>(
                    (file1, file2) => file1.LocalInstallPath == file2.LocalInstallPath,
                    file => file.LocalInstallPath.GetHashCode())
            );

            var packageReferences = new HashSet<ApplicationPackageReference>(
                EqualityComparer.Create<ApplicationPackageReference>(
                    (reference1, reference2) => string.Equals(reference1.PackageId, reference2.PackageId,
                        StringComparison.OrdinalIgnoreCase),
                    reference => reference.PackageId.GetHashCode())
            );

            using (var loader = new MetadataLoader(productBinariesDir))
            {
                ProcessAssembly(packages, productBinariesDir, loader, assemblyNameInfo, packageFiles,
                    packageReferences);
            }

            var packageArtifact = new ApplicationPackageArtifact(new SubplatformName(assemblyNameInfo.Name),
                new JetSemanticVersion(assemblyNameInfo.Version), "FakeCompanyName", "FakeCompanyName", DateTime.UtcNow,
                null, null, packageFiles, packageReferences);

// >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            // Add a whitelist artifact to show that it's ok if we can't resolve these assemblies. They don't exist in a
            // normal Mono install, and we have to manually copy them from the SDK to the bin folder for tests to run
            var whitelistArtifact =
                new SubplatformWhitelistMissingAssemblyReferencesArtifact.WhitelistMissingAssemblyReferences(
                    new List<AssemblyNameInfo>
                    {
                        AssemblyNameInfo.Parse(
                            "PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35"),
                        AssemblyNameInfo.Parse(
                            "PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
                    });

            var fiMetadata = Lifetime.Using(lifeResolver =>
            {
                // All components together
                var resolver = new SerializedValuesResolver(lifeResolver,
                    new IStructuredStorageSerializable[] {packageArtifact, whitelistArtifact});

                // Components in a file
                // NOTE: this file can't be a [Transformed]SubplatformFileForPackaging because the components are built out of the transformed files themselves, and this would require another level of post-transformed files, which we would not yet like to do
                return new SimpleFileItem(
                    NugetApplicationPackageConvention.GetJetMetadataEffectivePath(packageArtifact),
                    StructuredStorages.CreateMemoryStream(storage => resolver.GetObjectData(storage)));
            });
// <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<

            metafile.WriteStream(sout => fiMetadata.FileContent.CopyStream(sout));

            base.SetUp();
        }

#region Unchanged implementation of ExtensionTestEnvironmentAssembly
        public override bool IsRunningTestsWithAsyncBehaviorProhibited => true;

        protected override JetHostItems.Packages CreateJetHostPackages(JetHostItems.Engine engine)
        {
            var mainAssembly = GetType().Assembly;
            var productBinariesDir = mainAssembly.GetPath().Parent;

            TestUtil.SetHomeDir(mainAssembly);

            Lazy<ProductBinariesDirArtifact> productBinariesDirArtifact =
                Lazy.Of(() => new ProductBinariesDirArtifact(mainAssembly.GetPath().Directory), true);
            // ReSharper disable once UnusedVariable
            // ReSharper disable once IdentifierTypo
            var jethostitempackages = new JetHostItems.Packages(engine.Items.Concat(
#pragma warning disable 618
                new CollectProductPackagesInDirectoryFlatNoCachingHostMixin(productBinariesDirArtifact,
#pragma warning restore 618
                    // ReSharper disable once IdentifierTypo
                    allass => new[] {allass.FindSubplatformOfAssembly(mainAssembly.GetNameInfo(), OnError.Throw)},
                    packages =>
                    {
                        var packageFiles = new HashSet<ApplicationPackageFile>(
                            EqualityComparer.Create<ApplicationPackageFile>(
                                (file1, file2) => file1.LocalInstallPath == file2.LocalInstallPath,
                                file => file.LocalInstallPath.GetHashCode())
                        );

                        var packageReferences = new HashSet<ApplicationPackageReference>(
                            EqualityComparer.Create<ApplicationPackageReference>(
                                (reference1, reference2) => string.Equals(reference1.PackageId, reference2.PackageId,
                                    StringComparison.OrdinalIgnoreCase),
                                reference => reference.PackageId.GetHashCode())
                        );

                        var assemblyNameInfo = AssemblyNameInfo.Parse(mainAssembly.FullName);
                        using (var loader = new MetadataLoader(productBinariesDir))
                        {
                            ProcessAssembly(packages, productBinariesDir, loader, assemblyNameInfo, packageFiles,
                                packageReferences);
                        }

                        var packageArtifact = new ApplicationPackageArtifact(new SubplatformName(assemblyNameInfo.Name),
                            // ReSharper disable once AssignNullToNotNullAttribute
                            new JetSemanticVersion(assemblyNameInfo.Version), CompanyInfo.Name, CompanyInfo.NameWithInc,
                            DateTime.UtcNow, null, null, packageFiles, packageReferences);
                        return new AllAssembliesOnPackages(packages.Subplatforms
                            .Concat(new SubplatformOnPackage(packageArtifact, null, null)).AsCollection());
                    })));

            return base.CreateJetHostPackages(engine);
        }

        private static void ProcessAssembly(AllAssemblies allAssemblies, FileSystemPath productBinariesDir,
            MetadataLoader metadataLoader, AssemblyNameInfo assemblyNameInfo,
            HashSet<ApplicationPackageFile> packageFiles, HashSet<ApplicationPackageReference> packageReferences)
        {
            var assembly = metadataLoader.TryLoad(assemblyNameInfo, JetFunc<AssemblyNameInfo>.False, false);
            if (assembly == null) return;

            var subplatformOfAssembly = allAssemblies.FindSubplatformOfAssembly(assemblyNameInfo, OnError.Ignore);

            if (subplatformOfAssembly != null)
            {
                var subplatformReference = new ApplicationPackageReference(subplatformOfAssembly.Name,
                    subplatformOfAssembly.GetCompanyNameHuman());
                packageReferences.Add(subplatformReference);
                return;
            }

            if (!packageFiles.Add(new ApplicationPackageFile(assembly.Location.MakeRelativeTo(productBinariesDir),
                assemblyNameInfo)))
                return;

            foreach (var referencedAssembly in assembly.ReferencedAssembliesNames)
            {
                ProcessAssembly(allAssemblies, productBinariesDir, metadataLoader, referencedAssembly, packageFiles,
                    packageReferences);
            }
        }
#endregion
    }
}
