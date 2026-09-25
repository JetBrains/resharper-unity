#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Shaders.HlslSupport;

// RIDER-118787: ProcessFileSystemChangeDelta, called by reflection. In the test shell, the work that a real
// watcher event queues is dropped with AsyncBehaviorIsProhibitedException.
[TestFixture]
[Category("Cpp.HLSL")]
[RequireHlslSupport]
public class WatcherDeltaHandlingTest : PackageShaderTestBase
{
    [Test]
    public void AddedShaderUnderHiddenAssetFolderIsNotAdopted()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var root = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0");
            var hiddenPath = root.Combine(@"Documentation~/Ignored.hlsl");
            var visiblePath = root.Combine(@"Runtime/Shaders/NotOnDiskYet.urtshader");

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();

            var hidden = ProcessDelta(processor, root, "ADDED", hiddenPath, hiddenPath);
            Assert.IsFalse(hidden.ToCreate.Contains(hiddenPath),
                "Documentation~ is off limits to Unity, so a file appearing under it must never be queued for a project item.");

            // Control: an ordinary shader is queued.
            var visible = ProcessDelta(processor, root, "ADDED", visiblePath, visiblePath);
            Assert.IsTrue(visible.ToCreate.Contains(visiblePath),
                "Control failed: an ordinary new shader path should still be queued for a project item.");
        });
    }

    [Test]
    public void RenamedShaderIntoHiddenAssetFolderIsNotAdopted()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var root = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0");
            var oldPath = root.Combine(@"Runtime/Shaders/WasVisible.urtshader");
            var newPath = root.Combine(@"Documentation~/NowHidden.urtshader");

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();
            var result = ProcessDelta(processor, root, "RENAMED", oldPath, newPath);

            Assert.IsFalse(result.ToCreate.Contains(newPath),
                "A rename landing inside Documentation~ must not be adopted, same as the walk would never have found it there.");
        });
    }

    // Items are created directly, because waiting for the package walk is flaky here.
    [Test]
    public void DeletedAndRenamedShaderDropTheirProjectItemsAndForgetTheirPaths()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var root = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0");
            var deletedPath = root.Combine(@"Runtime/Shaders/Fog.urtshader");
            var renamedOldPath = root.Combine(@"Runtime/Shaders/FogHelpers.cginc");
            var renamedNewPath = root.Combine(@"Runtime/Shaders/FogHelpersRenamed.cginc");

            RefreshUnityPackages(solution);

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();
            InvokeVoid(processor, "CreateProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { deletedPath, renamedOldPath });

            Assert.IsTrue(HasRealProjectItem(solution, deletedPath), "Setup failed: no item to delete");
            Assert.IsTrue(HasRealProjectItem(solution, renamedOldPath), "Setup failed: no item to rename away from");

            // A second owned item at the same location: all of them must be removed.
            var secondOwnerProject = solution.GetAllProjects().Single(p => p.Name == "Assembly-CSharp");
            var secondOwner = (ProjectFolderImpl)secondOwnerProject;
            var origin = ((IProjectFile)FindRealProjectItem(solution, deletedPath)).Origin;
            var propertiesFactory = solution.GetComponent<ProjectFilePropertiesFactory>();
            secondOwner.DoCreateFile(deletedPath.Name, deletedPath,
                propertiesFactory.CreateProjectFileProperties(secondOwnerProject.ProjectProperties), origin);
            Assert.AreEqual(2, solution.FindRealProjectItemsByLocation(deletedPath).Count(),
                "Setup failed: expected two real items sharing this location");

            var deleted = ProcessDelta(processor, root, "DELETED", deletedPath, deletedPath);
            Assert.IsTrue(deleted.ToRemove.Contains(deletedPath),
                "A DELETED event for a path the creator handled must queue its item for removal.");

            InvokeVoid(processor, "RemoveProjectFiles", (IReadOnlyList<VirtualFileSystemPath>)deleted.ToRemove);

            Assert.IsFalse(HasRealProjectItem(solution, deletedPath),
                "The item survived the delete - RemoveProjectFiles did not actually remove it, or stopped after " +
                "removing only one of the two owned items sharing this location.");
            Assert.IsFalse(IsProjectFileOnlyPath(processor, deletedPath),
                "The path is still remembered after removal, so a later rename would try to clean it up again.");

            var renamed = ProcessDelta(processor, root, "RENAMED", renamedOldPath, renamedNewPath);
            Assert.IsTrue(renamed.ToRemove.Contains(renamedOldPath),
                "A RENAMED event must queue the old path's item for removal, or it and the path both leak.");
            Assert.IsTrue(renamed.ToCreate.Contains(renamedNewPath),
                "The new path is an ordinary shader location and should still be queued for its own item.");

            InvokeVoid(processor, "RemoveProjectFiles", (IReadOnlyList<VirtualFileSystemPath>)renamed.ToRemove);

            Assert.IsFalse(HasRealProjectItem(solution, renamedOldPath), "The old item survived the rename.");
            Assert.IsFalse(IsProjectFileOnlyPath(processor, renamedOldPath),
                "The old path is still remembered after the rename cleaned it up.");
        });
    }

    [Test]
    public void CaseOnlyRenameRemovesTheOldItemAndRecreatesItAtTheNewCasing()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var root = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0");
            var oldPath = root.Combine(@"Runtime/Extras/Shaders/Extra.urtshader");
            var newPath = root.Combine(@"Runtime/Extras/Shaders/EXTRA.urtshader");

            RefreshUnityPackages(solution);

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();
            InvokeVoid(processor, "CreateProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { oldPath });
            Assert.IsTrue(HasRealProjectItem(solution, oldPath), "Setup failed: no item to rename away from");

            var renamed = ProcessDelta(processor, root, "RENAMED", oldPath, newPath);
            Assert.IsTrue(renamed.ToRemove.Contains(oldPath),
                "The old casing's item must still be queued for removal.");
            Assert.IsTrue(renamed.ToCreate.Contains(newPath),
                "The new casing must be queued for a project item even though HasRealProjectItem(newPath) still " +
                "sees the not-yet-removed old item at this path.");

            // The same order as OnWatchedDirectoryChange.
            InvokeVoid(processor, "RemoveProjectFiles", (IReadOnlyList<VirtualFileSystemPath>)renamed.ToRemove);
            InvokeVoid(processor, "CreateProjectFiles", (IReadOnlyList<VirtualFileSystemPath>)renamed.ToCreate);

            Assert.IsTrue(HasRealProjectItem(solution, newPath),
                "The file has no project item after the rename - it silently went back to unanalysable.");
            Assert.IsTrue(IsProjectFileOnlyPath(processor, newPath),
                "The new casing must be remembered so a later reload restores its item.");
        });
    }

    // Fog.hlsl is listed in the Runtime .csproj, so its item is not ours to remove.
    [Test]
    public void CaseOnlyRenameOfARememberedPathDoesNotDeleteAGenuineProjectItem()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            var fogHlslPath = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/Fog.hlsl");
            var renamedPath = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/FOG.hlsl");

            var genuineItem = FindRealProjectItem(solution, fogHlslPath);
            Assert.NotNull(genuineItem,
                "Setup failed: Fog.hlsl should already have a descriptor-backed item from the .csproj import");

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();

            // Remembers the path without creating an item.
            InvokeVoid(processor, "CreateProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { fogHlslPath });
            Assert.IsTrue(IsProjectFileOnlyPath(processor, fogHlslPath),
                "Setup failed: Fog.hlsl should now be remembered as project-file-only.");
            Assert.AreSame(genuineItem, FindRealProjectItem(solution, fogHlslPath),
                "Setup failed: adopting an already-claimed path must not touch its existing item.");

            var renamed = ProcessDelta(processor, fogHlslPath.Parent, "RENAMED", fogHlslPath, renamedPath);
            Assert.IsTrue(renamed.ToRemove.Contains(fogHlslPath),
                "The old casing must still be queued for removal - telling the genuine item apart is " +
                "RemoveProjectFiles' job, not the queueing decision's.");

            InvokeVoid(processor, "RemoveProjectFiles", (IReadOnlyList<VirtualFileSystemPath>)renamed.ToRemove);

            var afterRemove = FindRealProjectItem(solution, fogHlslPath);
            Assert.NotNull(afterRemove,
                "Fog.hlsl's genuine, descriptor-backed project item was deleted - RemoveProjectFiles must only " +
                "remove items this creator made.");
            Assert.AreSame(genuineItem, afterRemove, "Fog.hlsl's project item was replaced rather than left alone.");

            InvokeVoid(processor, "CreateProjectFiles", (IReadOnlyList<VirtualFileSystemPath>)renamed.ToCreate);

            Assert.AreSame(genuineItem, FindRealProjectItem(solution, renamedPath),
                "The genuine item did not survive the full remove-then-create sequence - it ended up replaced " +
                "by an invented one.");
        });
    }

    // Needs a resolvable owner, or the path is rejected even without the guard.
    [Test]
    public void CreateProjectFilesSkipsAPathDeletedBeforeTheQueuedCallbackRuns()
    {
        DoTestSolution(GetTestDataFilePath2(@"PackageShadersGenerated\PackageShadersGenerated.sln"), (_, solution) =>
        {
            RefreshUnityPackages(solution);

            var existingPath = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/Fog.urtshader");
            var missingPath = solution.SolutionDirectory.Combine(
                @"Library/PackageCache/com.test.generatedshaders@1.0.0/Runtime/Shaders/GoneBeforeTheCallbackRan.urtshader");
            Assert.IsFalse(missingPath.ExistsFile, "Test setup relies on this path not existing on disk");

            var processor = solution.GetComponent<UnityExternalFilesModuleProcessor>();

            InvokeVoid(processor, "CreateProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { existingPath });
            var originalItem = FindRealProjectItem(solution, existingPath);
            Assert.NotNull(originalItem, "Setup failed: no item for the control to remove and recreate");

            // Not DoRemove, which would trigger the automatic recreate.
            InvokeVoid(processor, "RemoveProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { existingPath });
            Assert.IsFalse(HasRealProjectItem(solution, existingPath), "Setup failed: the item was not actually removed");

            // Control: an existing path is recreated.
            InvokeVoid(processor, "CreateProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { existingPath });
            var recreatedItem = FindRealProjectItem(solution, existingPath);
            Assert.NotNull(recreatedItem,
                "Control failed: an existing shader beneath a resolvable owner should get a project item.");
            Assert.AreNotSame(originalItem, recreatedItem,
                "Control failed: CreateProjectFiles must create a new item, not merely find the old one still there.");

            InvokeVoid(processor, "CreateProjectFiles",
                (IReadOnlyList<VirtualFileSystemPath>)new List<VirtualFileSystemPath> { missingPath });

            Assert.IsFalse(HasRealProjectItem(solution, missingPath),
                "A path deleted before the queued CreateProjectFiles callback ran must not get a project item.");
            Assert.IsFalse(IsProjectFileOnlyPath(processor, missingPath),
                "A path that no longer exists must not be remembered as project-file-only either.");
        });
    }

    private readonly struct DeltaResult
    {
        public readonly List<VirtualFileSystemPath> ToCreate;
        public readonly List<VirtualFileSystemPath> ToRemove;

        public DeltaResult(List<VirtualFileSystemPath> toCreate, List<VirtualFileSystemPath> toRemove)
        {
            ToCreate = toCreate;
            ToRemove = toRemove;
        }
    }

    private static DeltaResult ProcessDelta(UnityExternalFilesModuleProcessor processor, VirtualFileSystemPath root,
                                            string changeTypeName, VirtualFileSystemPath oldPath,
                                            VirtualFileSystemPath newPath)
    {
        var deltaType = ResolveRuntimeType("JetBrains.Application.changes.FileSystemChangeDelta");
        var changeTypeEnum = ResolveRuntimeType("JetBrains.Application.changes.FileSystemChangeType");
        var ctor = deltaType.GetConstructor(
            new[] { changeTypeEnum, typeof(VirtualFileSystemPath), typeof(VirtualFileSystemPath) });
        Assert.NotNull(ctor, "FileSystemChangeDelta's constructor shape changed - update this reflection helper");
        var changeTypeValue = Enum.Parse(changeTypeEnum, changeTypeName);
        var delta = ctor!.Invoke(new[] { changeTypeValue, (object)oldPath, newPath });

        var builderType = ResolveRuntimeType("JetBrains.ReSharper.Psi.Modules.PsiModuleChangeBuilder");
        var builder = Activator.CreateInstance(builderType)!;

        var toCreate = new List<VirtualFileSystemPath>();
        var toRemove = new List<VirtualFileSystemPath>();

        var method = typeof(UnityExternalFilesModuleProcessor).GetMethod("ProcessFileSystemChangeDelta", PrivateInstance);
        Assert.NotNull(method, "ProcessFileSystemChangeDelta not found by reflection - production signature changed?");
        method!.Invoke(processor, new object[] { root, delta!, builder, true, toCreate, toRemove });

        return new DeltaResult(toCreate, toRemove);
    }

    private static void InvokeVoid(UnityExternalFilesModuleProcessor processor, string methodName, object arg)
    {
        var method = typeof(UnityExternalFilesModuleProcessor).GetMethod(methodName, PrivateInstance);
        Assert.NotNull(method, $"{methodName} not found by reflection - production signature changed?");
        method!.Invoke(processor, new[] { arg });
    }

    private static Type ResolveRuntimeType(string fullName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? type;
            try { type = assembly.GetType(fullName); }
            catch { continue; }
            if (type != null)
                return type;
        }

        throw new InvalidOperationException($"Runtime type {fullName} not found - is a Unity solution loaded yet?");
    }
}
