using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRiderTestComponents;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Threading;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// Keep one solution-opening test per fixture: a second Unity solution in one fixture gets no package walk.
public abstract class PackageShaderTestBase : BaseTestWithExistingSolution
{
    protected override string RelativeTestDataPath => "Shaders";

    protected static void RefreshUnityPackages(ISolution solution) =>
        solution.GetComponent<TestPackageManager>().RefreshPackagesSynchronously();

    // Misc Files items do not count.
    protected static bool HasRealProjectItem(ISolution solution, VirtualFileSystemPath path) =>
        FindRealProjectItem(solution, path) != null;

    protected static IProjectItem FindRealProjectItem(ISolution solution, VirtualFileSystemPath path) =>
        solution.FindRealProjectItemsByLocation(path).FirstOrDefault();

    protected const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    protected static bool IsProjectFileOnlyPath(UnityExternalFilesModuleProcessor processor, VirtualFileSystemPath path)
    {
        var method = typeof(UnityExternalFilesModuleProcessor).GetMethod("IsProjectFileOnlyPath", PrivateInstance);
        Assert.NotNull(method, "IsProjectFileOnlyPath not found by reflection - production signature changed?");
        return (bool)method!.Invoke(processor, new object[] { path })!;
    }

    // Does not fail on timeout: the caller's assertion reports the failure.
    protected static void PumpUntil(IShellLocks locks, Func<bool> condition, TimeSpan timeout)
    {
        locks.ReentrancyGuard.AllowNestedExecution("RIDER-118787: wait for Unity project model", () =>
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline && !condition())
            {
                JetDispatcher.PumpMessagesOnce();
                Thread.Sleep(10);
            }
        });
    }
}
