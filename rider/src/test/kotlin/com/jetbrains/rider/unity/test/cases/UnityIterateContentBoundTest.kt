package com.jetbrains.rider.unity.test.cases

import com.intellij.openapi.application.runReadActionBlocking
import com.intellij.openapi.rd.util.lifetime
import com.intellij.openapi.roots.ProjectFileIndex
import com.intellij.openapi.vfs.VirtualFileFilter
import com.intellij.openapi.vfs.newvfs.NewVirtualFile
import com.intellij.openapi.vfs.newvfs.NewVirtualFileSystem
import com.intellij.openapi.vfs.newvfs.TransientVirtualFileImpl
import com.jetbrains.rdclient.util.idea.waitAndPump
import com.jetbrains.rider.plugins.unity.UnityProjectFileIndexAugmentor
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.test.OpenSolutionParams
import com.jetbrains.rider.test.annotations.Solution
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.TestSettings
import com.jetbrains.rider.test.annotations.report.ChecklistItems
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.annotations.report.Issue
import com.jetbrains.rider.test.annotations.report.Severity
import com.jetbrains.rider.test.annotations.report.SeverityLevel
import com.jetbrains.rider.test.enums.BuildTool
import com.jetbrains.rider.test.enums.sdk.SdkVersion
import com.jetbrains.rider.test.junit5.base.PerTestSolutionTestBase
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.time.Duration

/**
 * Covers [UnityProjectFileIndexAugmentor.skipFromIteration], the bound on what `iterateContent` enumerates for a Unity
 * solution (RIDER-141737).
 *
 * Asserts through [ProjectFileIndex] rather than by calling the augmentor, because the pruning is a collaboration: the
 * augmentor decides, `RiderProjectFileIndexImpl` turns that into a skipped subtree. Calling the predicate alone would
 * leave the wiring untested.
 *
 * `SimpleUnityProject` carries one file in each category that matters: `Library/zero` for the generated directories
 * the bound prunes, `ProjectSettings/ProjectVersion.txt` for a directory Unity writes but does *not* own the decision
 * about, `TitleScreen.png` for an asset owning no project-model entity, and a `.sln` and `.csproj` loose in the
 * solution folder.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity project file index augmentor")
@Severity(SeverityLevel.CRITICAL)
@Issue("RIDER-141737")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SimpleUnityProject")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityIterateContentBoundTest : PerTestSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        params.preprocessTempDirectory = { prepareAssemblies(it) }
    }

    private val index get() = ProjectFileIndex.getInstance(project)

    @Test
    @ChecklistItems(["Unity project file index / iterateContent yields assets but not generated directories"])
    fun iterateContentYieldsTheProjectsOwnFilesAndNothingGenerated() {
        awaitTheSolutionFolderBlanket()

        val iterated = iterateContentRelativePaths()

        // The defect this whole change exists for: assets owning no ProjectModelEntity must be enumerable.
        assertTrue("Assets/UI/TitleScreen.png" in iterated,
                   "An asset with no project-model entity of its own must be iterated; got $iterated")
        assertTrue("Assets/NewBehaviourScript.cs" in iterated,
                   "A script under Assets/ must be iterated; got $iterated")

        // The bound: Unity's generated directories are pruned, and pruned whole rather than merely unemitted.
        assertFalse(iterated.any { it == "Library" || it.startsWith("Library/") },
                    "Library/ is Unity's cache and must not be iterated; got $iterated")

        // And the bound stops there. ProjectSettings/ is written by Unity but checked in and edited, so it is not
        // Unity's cache and this code does not get a vote on it -- the same answer a non-Unity Rider solution gives
        // for any directory beside the .sln. The bound names what Unity generates; everything else is someone else's.
        assertTrue("ProjectSettings/ProjectVersion.txt" in iterated,
                   "ProjectSettings/ is not generated output and must be left to whoever owns it; got $iterated")

        // The bound prunes directories only, so the solution's own loose files keep being iterated as they always were.
        assertTrue("SimpleUnityProject.sln" in iterated,
                   "The solution file sits beside Assets/ rather than under it and must survive the bound; got $iterated")
        assertTrue("Assembly-CSharp.csproj" in iterated,
                   "A generated project file is still a project-model node and must survive the bound; got $iterated")
    }

    /**
     * `iterateContent` walks the solution folder wrapped as cache-avoiding, and
     * `CacheAvoidingVirtualFileWrapper.getChildren` hands out a `TransientVirtualFileImpl` for every child that is not
     * already cached — a shape whose `equals` is class-checked, so it is never equal to the plain `VirtualFile` a
     * `findChild` returns.
     *
     * The bound used to resolve `Assets` through `findChild` and compare instances, so a cold `Assets/` failed that
     * test and was pruned as junk — silently reinstating the very regression RIDER-141737 fixed. Asserted against the
     * augmentor directly rather than through a walk, because the fixture's VFS is warm by the time the solution
     * finishes opening and the shape cannot be provoked from outside.
     */
    @Test
    @ChecklistItems(["Unity project file index / a content root is recognised whatever VirtualFile shape it arrives in"])
    fun theBoundRecognisesAssetsWhenItArrivesAsACacheAvoidingTransient() {
        awaitTheSolutionFolderBlanket()

        val baseDir = project.projectDir
        runReadActionBlocking {
            val transientAssets = TransientVirtualFileImpl(
                "Assets",
                baseDir.path + "/Assets",
                baseDir.fileSystem as NewVirtualFileSystem,
                NewVirtualFile.asCacheAvoiding(baseDir)
            )
            assertTrue(transientAssets.isDirectory, "the fixture must have an Assets/ directory to stand in for")
            assertFalse(transientAssets == baseDir.findChild("Assets"),
                        "a transient must not equal its cached counterpart, or this test guards nothing")

            assertFalse(UnityProjectFileIndexAugmentor().skipFromIteration(project, index, transientAssets),
                        "Assets/ must never be pruned, whatever VirtualFile shape it arrives in")
        }
    }

    @Test
    @ChecklistItems(["Unity project file index / the bound survives a caller-supplied file filter"])
    fun theBoundHoldsWhenTheCallersFilterRejectsTheDirectory() {
        awaitTheSolutionFolderBlanket()

        // A filter that rejects every directory is the case that forced the bound and the caller's filter into the
        // same iterator: left with the platform, this filter would hide Library/ from the bound and the walk would
        // descend and emit the files below. Callers do pass filters here: AI chat's #file completion does.
        val iterated = iterateContentRelativePaths(VirtualFileFilter { !it.isDirectory })

        assertFalse(iterated.any { it.startsWith("Library/") },
                    "Library/ must stay pruned even when the caller's filter hides the directory itself; got $iterated")
        assertTrue("Assets/UI/TitleScreen.png" in iterated,
                   "The filter accepts files, so assets must still be iterated; got $iterated")
    }

    /**
     * `RiderProjectFileIndexImpl` applies the caller's filter inside its wrapping iterator, and asks it about every
     * node it visits -- including the ones the augmentor is about to prune. A stateful filter (the platform's own
     * `DeduplicatingVirtualFileFilter` is exactly this) relies on being told about every node it is shown, not only
     * the ones that end up emitted.
     */
    @Test
    @ChecklistItems(["Unity project file index / the caller's filter is asked about a node the augmentor is about to prune"])
    fun theCallersFilterIsAskedAboutLibraryEvenThoughTheAugmentorPrunesIt() {
        awaitTheSolutionFolderBlanket()

        val prefix = project.projectDir.path + "/"
        val asked = mutableSetOf<String>()
        runReadActionBlocking {
            index.iterateContent({ true }, VirtualFileFilter { file ->
                if (file.path.startsWith(prefix)) asked.add(file.path.removePrefix(prefix))
                false // reject everything -- if Library still gets pruned, that pruning came from skipFromIteration alone
            })
        }

        assertTrue("Library" in asked,
                   "the caller's filter must be consulted about Library even though it always rejects and the " +
                   "augmentor always prunes; got $asked")
    }

    /**
     * The solution folder's recursive content set is created asynchronously; until it exists there is no walk to bound
     * and the assertions would pass vacuously.
     *
     * `Library/zero` is the probe because only that set can make it content — the augmentor's widening reaches
     * `Assets/` and `Packages/` alone. Nothing re-registers file sets here, so once it is up the bound is in force.
     */
    private fun awaitTheSolutionFolderBlanket() {
        // isUnityProject is published asynchronously by UnityProjectDiscoverer, and the augmentor gates on it.
        waitAndPump(project.lifetime, { project.isUnityProject.value }, Duration.ofSeconds(30)) {
            "SimpleUnityProject was never detected as a Unity project"
        }
        val libraryFile = project.projectDir.findFileByRelativePath("Library/zero")
                          ?: error("Library/zero is missing from SimpleUnityProject")
        waitAndPump(project.lifetime, { runReadActionBlocking { index.isInContent(libraryFile) } },
                    Duration.ofSeconds(30)) {
            "The solution folder never became a recursive content root, so there is no walk to bound"
        }
    }

    /** Strings, not files: the walk hands out `CacheAvoidingVirtualFileWrapper`s whose `equals` is asymmetric. */
    private fun iterateContentRelativePaths(filter: VirtualFileFilter? = null): Set<String> {
        val prefix = project.projectDir.path + "/"
        val result = mutableSetOf<String>()
        runReadActionBlocking {
            index.iterateContent({ file ->
                if (file.path.startsWith(prefix)) result.add(file.path.removePrefix(prefix))
                true
            }, filter)
        }
        return result
    }
}
