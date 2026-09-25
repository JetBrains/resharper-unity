using System;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: an .asmdef under Assets must not adopt a shader. Only package shaders are adopted.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class AssetsAsmdefShaderIsNotAdoptedTest : PackageShaderTestBase
{
    [Test]
    public void ShaderUnderAnAssetsAsmdefIsNotAdopted()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            RefreshUnityPackages(solution);

            var locks = solution.GetComponent<IShellLocks>();
            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();

            // Control: proves the package walk ran.
            var controlPath = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/Fog.urtshader");
            PumpUntil(locks, () => HasRealProjectItem(solution, controlPath), TimeSpan.FromSeconds(15));
            Assert.IsTrue(HasRealProjectItem(solution, controlPath),
                "Control failed: a package-path shader should have been adopted in this run, so the negative " +
                "assertion below cannot be trusted.");
            Assert.IsTrue(IsProjectFileOnlyPath(processor, controlPath),
                "An adopted package shader must be remembered, or a reload cannot restore its item.");

            var path = solution.SolutionDirectory.Combine(@"Assets/Shaders/Unlisted/Marooned.urtshader");

            PumpUntil(locks, () => HasRealProjectItem(solution, path), TimeSpan.FromSeconds(5));

            Assert.IsFalse(HasRealProjectItem(solution, path),
                "A shader under an Assets asmdef must not be adopted: it sits outside every package.");

            Assert.IsFalse(IsProjectFileOnlyPath(processor, path),
                "A shader the creator turned down must not be remembered as project-file-only.");
        });
    }
}
