using System;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: a second CollectInitialFiles run ("Turn On Anyway" for asset indexing) must keep our paths remembered.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class PackageShaderRescanTest : PackageShaderTestBase
{
    private static readonly TimeSpan ourTimeout = TimeSpan.FromSeconds(15);

    [Test]
    public void ASurvivingItemIsStillRememberedAfterASecondCollectionRunAndCanStillBeRecreated()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            var path = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/Fog.urtshader");

            RefreshUnityPackages(solution);
            PumpUntil(locks, () => HasRealProjectItem(solution, path), ourTimeout);

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();
            var before = FindRealProjectItem(solution, path);
            Assert.NotNull(before, "Setup failed: no item to survive a rescan");
            Assert.IsTrue(IsProjectFileOnlyPath(processor, path), "Setup failed: not remembered after the first collection run");

            var method = typeof(UnityExternalFilesModuleProcessor).GetMethod("CollectInitialFiles", PrivateInstance);
            Assert.NotNull(method, "CollectInitialFiles not found by reflection - production signature changed?");
            method!.Invoke(processor, new object[] { false });

            var afterRescan = FindRealProjectItem(solution, path);
            Assert.AreSame(before, afterRescan, "The item was recreated by the rescan even though nothing changed.");
            Assert.IsTrue(IsProjectFileOnlyPath(processor, path),
                "A surviving item must still be remembered after a second collection run, or a later sync-driven " +
                "removal has nothing left to recreate it from.");

            // A simulated sync must still recreate the item.
            ((ProjectItemBase)before).DoRemove();

            PumpUntil(locks,
                () => FindRealProjectItem(solution, path) is { } item && !ReferenceEquals(item, before), ourTimeout);

            var afterRemove = FindRealProjectItem(solution, path);
            Assert.NotNull(afterRemove,
                "The item was not recreated after removal - the rescan must have silently forgotten the path.");
            Assert.AreNotSame(before, afterRemove, "Still holds the original, now-removed item.");
        });
    }
}
