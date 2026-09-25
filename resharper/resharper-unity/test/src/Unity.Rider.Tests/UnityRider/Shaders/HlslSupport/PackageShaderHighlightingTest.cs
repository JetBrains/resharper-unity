using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Packages;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: package shader files that no generated .csproj lists get semantic highlighting.
// All HLSL bodies are identical, so project membership is the only variable. Local.hlsl and Fog.hlsl are the controls.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class PackageShaderHighlightingTest : PackageShaderTestBase
{
    // The golds alone do not show which project owns a file.
    private static readonly Dictionary<string, string> ourExpectedOwners = new()
    {
        { "Local.hlsl", "Assembly-CSharp" },
        { "Fog.hlsl", "com.test.generatedshaders.Runtime" },
        { "Fog.urtshader", "com.test.generatedshaders.Runtime" },
        { "FogHelpers.cginc", "com.test.generatedshaders.Runtime" },
        // No .asmdef above it: the package-wide fallback.
        { "Standalone.shader", "com.test.generatedshaders.Runtime" },
        // The nearest .asmdef wins.
        { "Extra.urtshader", "com.test.generatedshaders.Extras" },
        // Only the .Player twin claims Locked.asmdef, so the search falls through to Runtime.
        { "Guarded.urtshader", "com.test.generatedshaders.Runtime" }
    };

    // The files that no .csproj lists.
    private static readonly string[] ourCreatedFiles =
        { "Fog.urtshader", "FogHelpers.cginc", "Standalone.shader", "Extra.urtshader", "Guarded.urtshader" };

    [Test]
    public void ShaderFilesTheUnityGeneratorMissedAreSemanticallyHighlighted() =>
        DoHighlightingTestSolution(
            @"PackageShadersGenerated\PackageShadersGenerated.sln",
            @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/Fog.urtshader",
            "Local.hlsl", "Fog.hlsl", "Fog.urtshader", "FogHelpers.cginc", "Standalone.shader", "Extra.urtshader",
            "Guarded.urtshader");

    private void DoHighlightingTestSolution(string relativeSolutionPath, string relativePathToWaitFor,
                                            params string[] filesToHighlight)
    {
        DoTestSolution(GetTestDataFilePath2(relativeSolutionPath), (_, solution) =>
        {
            WaitForUnityPackages(solution, relativePathToWaitFor);

            var notFound = new HashSet<string>(filesToHighlight);

            // Unity lists a file in both an assembly and its .Player twin.
            foreach (var projectFile in solution.GetAllProjects()
                         .Where(project => !project.IsPlayerProject())
                         .SelectMany(project => project.GetAllProjectFiles())
                         .Where(projectFile => filesToHighlight.Contains(projectFile.Name)))
            {
                notFound.Remove(projectFile.Name);

                Assert.AreEqual(ourExpectedOwners[projectFile.Name], projectFile.GetProject().NotNull().Name,
                    $"{projectFile.Name} landed in the wrong project");

                ExecuteWithGold(projectFile, writer =>
                {
                    var dumper = new TestHighlightingDumper(projectFile.ToSourceFile().NotNull(), writer,
                        HighlightingPredicate);
                    dumper.DoHighlighting(DaemonProcessKind.VISIBLE_DOCUMENT);
                    dumper.Dump();
                });
            }

            Assert.IsEmpty(notFound, "Some files not found");

            // A created file must never land in a .Player project.
            foreach (var name in ourCreatedFiles)
            {
                var playerOwners = solution.GetAllProjects()
                    .Where(project => project.IsPlayerProject())
                    .SelectMany(project => project.GetAllProjectFiles())
                    .Where(projectFile => projectFile.Name == name)
                    .Select(projectFile => projectFile.GetProject().NotNull().Name)
                    .ToList();

                Assert.IsEmpty(playerOwners, $"{name} was adopted by a .Player project");
            }
        });
    }

    private static void WaitForUnityPackages(ISolution solution, string relativePathToWaitFor)
    {
        var locks = solution.GetComponent<IShellLocks>();
        var packageManager = solution.GetComponent<PackageManager>();
        var tracker = solution.GetComponent<UnitySolutionTracker>();

        RefreshUnityPackages(solution);

        Assert.IsTrue(packageManager.IsInitialUpdateFinished.Value,
            $"Unity package discovery never finished. IsUnityProjectFolder={tracker.IsUnityProjectFolder.Value}, " +
            $"IsUnityProject={tracker.IsUnityProject.Value}, HasUnityReference={tracker.HasUnityReference.Value}, " +
            $"SolutionDirectory={solution.SolutionDirectory}");

        // A timeout is fine: "Some files not found" below reports the failure.
        var waitPath = solution.SolutionDirectory.Combine(relativePathToWaitFor);
        PumpUntil(locks, () => HasRealProjectItem(solution, waitPath), TimeSpan.FromSeconds(15));
    }

    // Semantic identifier highlighting only: the bug removes it.
    private static bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                              IContextBoundSettingsStore settingsStore) =>
        highlighting is CppIdentifierHighlightingBase;
}
