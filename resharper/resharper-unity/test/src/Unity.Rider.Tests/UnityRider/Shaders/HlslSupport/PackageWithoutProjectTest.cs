using System;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: a package without a generated project gets no shader project items.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class PackageWithoutProjectTest : PackageShaderTestBase
{
    [Test]
    public void PackageWithNoProjectOfItsOwnIsLeftAlone()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShaders\PackageShaders.sln"), (_, solution) =>
        {
            RefreshUnityPackages(solution);

            var fogPath = solution.SolutionDirectory
                .Combine(@"Library/PackageCache/com.test.shaders@1.0.0/Shaders/Fog.hlsl");

            PumpUntil(solution.GetComponent<IShellLocks>(), () => HasRealProjectItem(solution, fogPath),
                TimeSpan.FromSeconds(5));

            Assert.IsFalse(HasRealProjectItem(solution, fogPath),
                "A package with no generated project of its own must not receive shader project items.");

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();
            Assert.IsFalse(IsProjectFileOnlyPath(processor, fogPath),
                "A shader from a project-less package must not be remembered as project-file-only.");
        });
    }
}
