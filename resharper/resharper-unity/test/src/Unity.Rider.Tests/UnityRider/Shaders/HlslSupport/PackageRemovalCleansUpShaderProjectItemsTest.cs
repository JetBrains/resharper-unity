using System;
using System.Collections;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: a package removal drops our items, forgets their paths, and stops watching the folder.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class PackageRemovalCleansUpShaderProjectItemsTest : PackageShaderTestBase
{
    [Test]
    public void RemovingAPackageDropsItsShaderProjectItemsTheRememberedPathsAndTheWatchedRoot()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            var packageFolder = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0");
            var path = packageFolder.Combine(@"Runtime/Shaders/Fog.urtshader");

            RefreshUnityPackages(solution);
            PumpUntil(locks, () => HasRealProjectItem(solution, path), TimeSpan.FromSeconds(15));

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();
            Assert.IsTrue(HasRealProjectItem(solution, path), "Setup failed: no item for the package removal to clean up");
            Assert.IsTrue(IsProjectFileOnlyPath(processor, path), "Setup failed: the item's path was never remembered");
            Assert.IsTrue(IsWatchedRoot(processor, packageFolder), "Setup failed: the package folder was never watched");

            // Called directly, because the refresh timer does not tick in the test shell.
            var packageManager = solution.GetComponent<PackageManager>();
            var removeMethod = typeof(PackageManager).GetMethod("RemovePackage", PrivateInstance);
            Assert.NotNull(removeMethod, "PackageManager.RemovePackage not found by reflection - production signature changed?");
            removeMethod!.Invoke(packageManager, new object[] { "com.test.generatedshaders" });

            Assert.IsFalse(HasRealProjectItem(solution, path),
                "The shader's project item survived its package being removed.");
            Assert.IsFalse(IsProjectFileOnlyPath(processor, path),
                "The path is still remembered after its package was removed, so a later REMOVED event for an " +
                "unrelated file at the same path would be mistaken for one of ours.");
            Assert.IsFalse(IsWatchedRoot(processor, packageFolder),
                "The package folder is still being watched after its package was removed.");
        });
    }

    private static bool IsWatchedRoot(UnityExternalFilesModuleProcessor processor, VirtualFileSystemPath root)
    {
        var field = typeof(UnityExternalFilesModuleProcessor).GetField("myRootPathLifetimes", PrivateInstance);
        Assert.NotNull(field, "myRootPathLifetimes not found by reflection - production field renamed?");
        var lifetimes = (IDictionary)field!.GetValue(processor)!;
        return lifetimes.Contains(root);
    }
}
