using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Xml.Linq;
using HarmonyLib;
using JetBrains.Application;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Application;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.HostParameters;
using JetBrains.Application.platforms;
using JetBrains.Build.Serialization;
using JetBrains.Extension;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Utils;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using JetBrains.Util.Collections;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.Util.Storage;
using JetBrains.Util.Storage.Packages;
using JetBrains.Util.Storage.StructuredStorage;
using NuGet;

namespace JetBrains.ReSharper.Plugins.Unity.Tests
{
    // These hacks change the platform. They are temporary, until the changes have been made in the actual SDK
    public static class TemporaryPlatformHacks
    {
        public static void ApplyPatches()
        {
            var harmony = new Harmony("com.jetbrains.resharper.tests::temporary_hacks");

            // Original method expands environment variables AFTER checking to see if it's a full or relative path
            var originalMethod = AccessTools.Method(typeof(JetNuGetSettingsV2), "ElementToValue");
            var prefixMethod = new HarmonyMethod(AccessTools.Method(typeof(TemporaryPlatformHacks),
                nameof(New_JetNuGetSettingsV2_ElementToValue)));
            harmony.Patch(originalMethod, prefixMethod);

            // Original method checks for running under Windows
            originalMethod = AccessTools.Method(typeof(NetPlatformsProvider), "GetPlatformsForShell");
            prefixMethod = new HarmonyMethod(AccessTools.Method(typeof(TemporaryPlatformHacks),
                nameof(New_NetPlatformsProvider_GetPlatformsForShell)));
            harmony.Patch(originalMethod, prefixMethod);

            harmony.CreateReversePatcher(AccessTools.Method(typeof(NetPlatformsProvider), "PlatformInfoFilter"),
                new HarmonyMethod(AccessTools.Method(typeof(TemporaryPlatformHacks), nameof(NetPlatformsProvider_PlatformInfoFilter)))).Patch();

            // Original method checks for running under Windows
            originalMethod = AccessTools.Method(typeof(NetPlatformsProvider), "DetectPlatformIdByReferences");
            prefixMethod = new HarmonyMethod(AccessTools.Method(typeof(TemporaryPlatformHacks),
                nameof(New_NetPlatformsProvider_DetectPlatformIdByReferences)));
            harmony.Patch(originalMethod, prefixMethod);

            // Original method checks for running under Windows
            originalMethod = AccessTools.Method(typeof(FrameworkLocationHelperBase), "GetDotNetFrameworkPlatforms");
            var transpilerMethod = new HarmonyMethod(AccessTools.Method(typeof(TemporaryPlatformHacks), nameof(Transpiler)));
            harmony.Patch(originalMethod, transpiler: transpilerMethod);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // IL_0000: ldsfld       bool [JetBrains.Platform.Core]JetBrains.Util.PlatformUtil::IsRunningUnderWindows
            // IL_0005: brtrue.s     IL_000d
            // IL_0007: ldsfld       class [mscorlib]System.Collections.Generic.IReadOnlyCollection`1<!0/*class JetBrains.Application.platforms.PlatformInfo*/> class [JetBrains.Platform.Core]JetBrains.Util.EmptyList`1<class JetBrains.Application.platforms.PlatformInfo>::Collection
            // IL_000c: ret
            var codes =new List<CodeInstruction>(instructions);
            if (codes.Count >= 4 && codes[0].opcode == OpCodes.Ldsfld && codes[1].opcode == OpCodes.Brtrue_S
                && codes[2].opcode == OpCodes.Ldsfld && codes[3].opcode == OpCodes.Ret)
            {
                codes.RemoveRange(0, 4);
            }

            return codes;
        }

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

        private static bool New_NetPlatformsProvider_GetPlatformsForShell(NetPlatformsProvider __instance, FrameworkLocationService ___myFrameworkLocationService, ref IReadOnlyCollection<PlatformInfo> __result)
        {
            __result = ___myFrameworkLocationService.Current.Value.GetDotNetFrameworkPlatforms()
                .Where(x => NetPlatformsProvider_PlatformInfoFilter(__instance, x)).ToIReadOnlyList();
            return false;
        }

        private static bool NetPlatformsProvider_PlatformInfoFilter(object instance, PlatformInfo platformInfo)
        {
            throw new InvalidOperationException("Stub method replaced at runtime");
        }

        private static bool New_NetPlatformsProvider_DetectPlatformIdByReferences(AssemblyNameInfo cr,
            IReadOnlyCollection<AssemblyNameInfo> otherReferences,
            IReadOnlyCollection<PlatformInfo> platforms,
            ref TargetFrameworkId __result)
        {
            __result = NetPlatformsProviderUtil.DetectPlatformIdByReferences(cr, otherReferences, platforms);
            return false;
        }
    }

    public abstract class Temporary_ExtensionTestEnvironmentAssembly<TTestEnvironmentZone> : TestEnvironmentAssembly<TTestEnvironmentZone>
        where TTestEnvironmentZone : ITestsEnvZone
    {
        protected virtual IEnumerable<IStructuredStorageSerializable> GetArtifacts(ApplicationPackageArtifact packageArtifact)
        {
            yield return packageArtifact;
            yield return GetWhitelistMissingAssemblyReferencesArtifact();
        }

        // This artifact lists the assemblies that are ok to be missing from the catalog. This is usually references
        // that won't be run on this platform, such as WPF on Mac.
        private IStructuredStorageSerializable GetWhitelistMissingAssemblyReferencesArtifact()
        {
            return new SubplatformWhitelistMissingAssemblyReferencesArtifact.WhitelistMissingAssemblyReferences(
                GetWhitelistMissingAssemblyReferences());
        }

        protected virtual IEnumerable<AssemblyNameInfo> GetWhitelistMissingAssemblyReferences()
        {
            yield return AssemblyNameInfo.Parse("PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            yield return AssemblyNameInfo.Parse("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            // Referenced via ReSharper SDK
            yield return AssemblyNameInfo.Parse("PresentationCore, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            yield return AssemblyNameInfo.Parse("PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            yield return AssemblyNameInfo.Parse("EnvDTE, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("EnvDTE80, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("Microsoft.VisualStudio.CommandBars, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("Microsoft.VisualStudio.ComponentModelHost, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("Microsoft.VisualStudio.OLE.Interop, Version=7.1.40304.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("Microsoft.VisualStudio.Shell.Interop.8.0, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("Microsoft.VisualStudio.TextManager.Interop, Version=7.1.40304.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("Microsoft.VisualStudio.VSHelp80, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("VSLangProj, Version=7.0.3300.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            yield return AssemblyNameInfo.Parse("System.Printing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
            yield return AssemblyNameInfo.Parse("UIAutomationClient, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            yield return AssemblyNameInfo.Parse("UIAutomationProvider, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
            yield return AssemblyNameInfo.Parse("UIAutomationTypes, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756");
        }

#region (Mostly) unchanged implementation of ExtensionTestEnvironmentAssembly
        // The only changes are to call the new methods above
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

            var artifacts = GetArtifacts(packageArtifact);

            var fiMetadata = Lifetime.Using(lifeResolver =>
            {
                // All components together
                var resolver = new SerializedValuesResolver(lifeResolver, artifacts);

                // Components in a file
                // NOTE: this file can't be a [Transformed]SubplatformFileForPackaging because the components are built out of the transformed files themselves, and this would require another level of post-transformed files, which we would not yet like to do
                return new SimpleFileItem(
                    NugetApplicationPackageConvention.GetJetMetadataEffectivePath(packageArtifact),
                    StructuredStorages.CreateMemoryStream(storage => resolver.GetObjectData(storage)));
            });

            metafile.WriteStream(sout => fiMetadata.FileContent.CopyStream(sout));

            base.SetUp();
        }

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