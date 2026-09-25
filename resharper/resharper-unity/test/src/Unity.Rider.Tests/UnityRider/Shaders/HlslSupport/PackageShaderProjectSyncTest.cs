using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: a full project sync drops our in-memory items, and they must come back.
// DoRemove simulates the sync, because a real MSBuild sync is not available in this test shell.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class PackageShaderProjectSyncTest : PackageShaderTestBase
{
    private static readonly TimeSpan ourTimeout = TimeSpan.FromSeconds(15);

    // Files that no .csproj lists. A sync restores the listed ones by itself.
    private static readonly string[] ourCreatedFiles =
    {
        @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/Fog.urtshader",
        @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/FogHelpers.cginc",
        @"Library/PackageCache/com.test.generatedshaders@1.0.0/Shaders/Standalone.shader"
    };

    [Test]
    public void ShaderProjectItemsAreRecreatedAfterAProjectModelSyncDropsThem()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var locks = solution.GetComponent<IShellLocks>();
            var externalFilesModule = solution.GetComponent<UnityExternalFilesModuleFactory>().PsiModule;
            var paths = ourCreatedFiles.Select(file => solution.SolutionDirectory.Combine(file)).ToList();

            RefreshUnityPackages(solution);

            foreach (var path in paths)
                PumpUntil(locks, () => HasRealProjectItem(solution, path), ourTimeout);

            // Read the owners now: a removed item has no project.
            var before = new Dictionary<VirtualFileSystemPath, IProjectItem>();
            var ownerBefore = new Dictionary<VirtualFileSystemPath, string>();
            foreach (var path in paths)
            {
                var item = FindRealProjectItem(solution, path);
                Assert.NotNull(item, $"{path.Name} never got a project item, so there is nothing for a sync to drop");
                before.Add(path, item);
                ownerBefore.Add(path, item.GetProject().NotNull().Name);
            }

            foreach (var item in before.Values)
                ((ProjectItemBase)item).DoRemove();

            foreach (var path in paths)
            {
                PumpUntil(locks,
                    () => FindRealProjectItem(solution, path) is { } item && !ReferenceEquals(item, before[path]),
                    ourTimeout);

                var after = FindRealProjectItem(solution, path);
                Assert.NotNull(after,
                    $"{path.Name} was not given a new project item after the sync dropped the old one, so its " +
                    "analysis dies on the first project reload");

                Assert.AreNotSame(before[path], after, $"{path.Name} still holds its original project item");
                Assert.AreEqual(ownerBefore[path], after.GetProject().NotNull().Name,
                    $"{path.Name} came back in a different project");
                Assert.IsFalse(externalFilesModule.ContainsPath(path),
                    $"{path.Name} was adopted by the Unity external files module instead of being recreated");
            }
        });
    }
}
