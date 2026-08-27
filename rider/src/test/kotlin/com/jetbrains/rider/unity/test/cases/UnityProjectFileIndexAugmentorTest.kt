package com.jetbrains.rider.unity.test.cases

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.runReadActionBlocking
import com.intellij.openapi.application.runWriteAction
import com.intellij.openapi.rd.util.lifetime
import com.intellij.openapi.roots.ProjectFileIndex
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.openapi.vfs.VirtualFile
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
import com.jetbrains.rider.test.scriptingApi.refreshFileSystem
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import com.jetbrains.rider.unity.test.framework.api.prepareAssemblies
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.time.Duration
import kotlin.io.path.createDirectories
import kotlin.io.path.writeText

/**
 * Covers [UnityProjectFileIndexAugmentor]'s memoization of `Project.projectDir` (RIDER-141491).
 *
 * The patch is a pure memoization, so no *answer* changed and a behaviour test would have passed before it too. These
 * tests therefore observe the memoized field, which is why it is `@VisibleForTesting`.
 *
 * `SimpleUnityProject` is the fixture because it is the only Unity project here with no `Packages/` directory, which
 * [packagesDirectoryAppearingLaterIsStillFound] needs.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity project file index augmentor")
@Severity(SeverityLevel.CRITICAL)
@Issue("RIDER-141491")
@TestSettings(sdkVersion = SdkVersion.LATEST_STABLE, buildTool = BuildTool.SDK)
@Solution("SimpleUnityProject")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityProjectFileIndexAugmentorTest : PerTestSolutionTestBase() {
    override fun modifyOpenSolutionParams(params: OpenSolutionParams) {
        params.preprocessTempDirectory = { prepareAssemblies(it) }
    }

    private val index get() = ProjectFileIndex.getInstance(project)

    @Test // the first call resolves projectDir; every later one reads the field instead of resolving again
    @ChecklistItems(["Unity project file index / projectDir is memoized per project"])
    fun projectDirIsMemoizedAndTheHitPathReadsIt() {
        val augmentor = newAugmentor()
        val script = assetScript()
        val assetsRoot = requireChild("Assets")

        assertNull(augmentor.cachedProjectDir, "Nothing should be resolved before the first call")

        val firstAnswer = runReadActionBlocking { augmentor.getContentRootForFile(project, index, script, true, null) }
        assertEquals(assetsRoot, firstAnswer, "Assets/ is the Unity content root for a file under it")
        assertEquals(project.projectDir, augmentor.cachedProjectDir, "The first call must memoize the project directory")

        // Plant a valid but wrong directory: if the hit path still resolved projectDir the answer would not move, but
        // because it reads the field the augmentor now looks for Assets/ under Library/ and finds none.
        val decoy = requireChild("Library")
        augmentor.cachedProjectDir = decoy

        val secondAnswer = runReadActionBlocking { augmentor.getContentRootForFile(project, index, script, true, null) }
        assertNull(secondAnswer, "The hit path must read the memoized directory rather than resolve projectDir again")
        assertEquals(decoy, augmentor.cachedProjectDir, "A valid cached directory must not be replaced")
    }

    @Test // isValid is the whole invalidation story: a stale cached directory is dropped and re-resolved
    @ChecklistItems(["Unity project file index / invalidated projectDir is re-resolved"])
    fun invalidatedProjectDirIsDiscardedAndReResolved() {
        val augmentor = newAugmentor()
        val script = assetScript()
        val assetsRoot = requireChild("Assets")

        // A deleted VirtualFile stands in for the post-VFS-reconnect "alien" file the guard really aims at: isValid
        // answers false for both, and is the one accessor that answers rather than throwing.
        val stale = createThenDeleteStaleDirectory()
        assertFalse(stale.isValid, "A deleted directory must report itself invalid")
        augmentor.cachedProjectDir = stale

        val answer = runReadActionBlocking { augmentor.getContentRootForFile(project, index, script, true, null) }
        assertEquals(assetsRoot, answer, "An invalid cached directory must be discarded and projectDir re-resolved")
        assertEquals(project.projectDir, augmentor.cachedProjectDir, "The re-resolved directory must replace the stale one")
    }

    @Test // the Unity roots are deliberately NOT memoized alongside projectDir — the VFS already caches them
    @ChecklistItems(["Unity project file index / Packages created after first use is recognised"])
    fun packagesDirectoryAppearingLaterIsStillFound() {
        val augmentor = newAugmentor()

        assertTrue(runReadActionBlocking { augmentor.isInProject(project, index, assetScript(), false) },
                   "A file under Assets/ must be in the project")
        assertNull(project.projectDir.findChild("Packages"),
                   "SimpleUnityProject is this test's fixture precisely because it starts with no Packages/")

        activeSolutionDirectory.resolve("Packages").createDirectories()
        activeSolutionDirectory.resolve("Packages/manifest.json").writeText("{\n  \"dependencies\": {}\n}\n")
        refreshFileSystem(project)

        val manifest = project.projectDir.findFileByRelativePath("Packages/manifest.json")
                       ?: error("VFS did not pick up the newly created Packages/manifest.json")

        assertTrue(runReadActionBlocking { augmentor.isInProject(project, index, manifest, false) },
                   "A Packages/ directory created after the augmentor first resolved its roots must still be " +
                   "recognised; this fails if the Unity roots are ever memoized alongside the project directory")
    }

    private fun newAugmentor(): UnityProjectFileIndexAugmentor {
        // isUnityProject is published asynchronously by UnityProjectDiscoverer; every augmentor entry point gates on it.
        waitAndPump(project.lifetime, { project.isUnityProject.value }, Duration.ofSeconds(30)) {
            "SimpleUnityProject was never detected as a Unity project"
        }
        return UnityProjectFileIndexAugmentor()
    }

    private fun requireChild(name: String): VirtualFile =
        project.projectDir.findChild(name) ?: error("$name/ is missing from SimpleUnityProject")

    private fun assetScript(): VirtualFile =
        project.projectDir.findFileByRelativePath("Assets/NewBehaviourScript.cs")
        ?: error("Assets/NewBehaviourScript.cs is missing from SimpleUnityProject")

    private fun createThenDeleteStaleDirectory(): VirtualFile {
        val path = activeSolutionDirectory.resolve("stale-project-dir")
        path.createDirectories()
        val file = LocalFileSystem.getInstance().refreshAndFindFileByNioFile(path)
                   ?: error("VFS did not pick up $path")
        ApplicationManager.getApplication().invokeAndWait { runWriteAction { file.delete(this) } }
        return file
    }
}
