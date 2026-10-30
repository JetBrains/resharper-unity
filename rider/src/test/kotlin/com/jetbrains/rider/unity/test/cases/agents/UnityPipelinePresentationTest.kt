package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityOldEditor
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelinePresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineRegistry
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineRelease
import com.jetbrains.rider.plugins.unity.settings.agents.unityEditorIsOlder
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineHasNewerVersion
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineLowestMinimumEditor
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineReleaseFor
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

/**
 * Pins the offer ladder of the Unity Pipeline Package section. The class reads no file and contacts no registry.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityPipelinePresentationTest {
    @Test
    fun `the offer ladder resolves in priority order`() {
        assertEquals(
            UnityPipelineOffer.BUSY,
            UnityPipelinePresentation(
                operation = UnityPipelineOperation.INSTALLING,
                failure = UnityPipelineFailure.REGISTRY_UNREACHABLE,
            ).offer,
            "an active operation takes precedence over failure",
        )
        assertEquals(
            UnityPipelineOffer.GATED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.NO_CLI,
                failure = UnityPipelineFailure.REGISTRY_UNREACHABLE,
            ).offer,
            "a closed gate takes precedence over a failure, because a retry cannot run",
        )
        assertEquals(
            UnityPipelineOffer.FAILED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                failure = UnityPipelineFailure.REGISTRY_UNREACHABLE,
            ).offer,
            "a failure takes precedence over the offer of a ready project",
        )
        assertEquals(
            UnityPipelineOffer.CHECKING,
            UnityPipelinePresentation(gate = UnityMcpSectionState.CHECKING).offer,
        )
        assertEquals(
            UnityPipelineOffer.GATED,
            UnityPipelinePresentation(gate = UnityMcpSectionState.NO_PROJECT).offer,
        )
        assertEquals(
            UnityPipelineOffer.GATED,
            UnityPipelinePresentation(gate = UnityMcpSectionState.NOT_TRUSTED).offer,
        )
        assertEquals(
            UnityPipelineOffer.GATED,
            UnityPipelinePresentation(gate = UnityMcpSectionState.NO_CLI).offer,
        )
        assertEquals(
            UnityPipelineOffer.GATED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.NO_CLI,
                manifest = UnityPipelineManifest(version = null),
            ).offer,
            "without the Unity CLI, Rider cannot add an absent package",
        )
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.NO_CLI,
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1"),
            ).offer,
            "the manifest shows the package without the Unity CLI",
        )
        assertEquals(
            UnityPipelineOffer.UNREADABLE,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = null,
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.INSTALL,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.UPGRADE,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.7.0-exp.1"),
                latestVersion = "0.8.0-exp.1",
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1"),
                latestVersion = "0.8.0-exp.1",
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1"),
                latestVersion = null,
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "https://github.com/x/y.git"),
                latestVersion = "0.8.0-exp.1",
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "file:../pipeline"),
                latestVersion = "0.8.0-exp.1",
            ).offer,
        )
    }

    @Test
    fun `newer version check only answers true when latest is newer than installed`() {
        assertTrue(unityPipelineHasNewerVersion("0.8.0-exp.1", "0.7.0-exp.1"))
        assertFalse(unityPipelineHasNewerVersion("0.8.0-exp.1", "0.8.0-exp.1"))
        assertFalse(unityPipelineHasNewerVersion("0.7.0-exp.1", "0.8.0-exp.1"))
        assertFalse(unityPipelineHasNewerVersion("0.8.0-exp.1", "https://github.com/x/y.git"))
        assertFalse(unityPipelineHasNewerVersion("not a version", "0.7.0-exp.1"))
    }

    @Test
    fun `needs pipeline package answers true for absent version, false for present, and null for unreadable`() {
        assertEquals(
            true,
            UnityPipelinePresentation(manifest = UnityPipelineManifest(version = null)).needsPipelinePackage,
        )
        assertEquals(
            false,
            UnityPipelinePresentation(manifest = UnityPipelineManifest(version = "0.8.0-exp.1")).needsPipelinePackage,
        )
        assertNull(
            UnityPipelinePresentation(manifest = null).needsPipelinePackage,
        )
    }

    @Test
    fun `every offer value is reached by at least one case`() {
        val presentations = listOf(
            UnityPipelinePresentation(operation = UnityPipelineOperation.INSTALLING),
            UnityPipelinePresentation(failure = UnityPipelineFailure.REGISTRY_UNREACHABLE),
            UnityPipelinePresentation(gate = UnityMcpSectionState.CHECKING),
            UnityPipelinePresentation(gate = UnityMcpSectionState.NO_PROJECT),
            UnityPipelinePresentation(gate = UnityMcpSectionState.READY, manifest = null),
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ),
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
            ),
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.7.0-exp.1"),
                latestVersion = "0.8.0-exp.1",
            ),
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1"),
                latestVersion = "0.8.0-exp.1",
            ),
        )
        assertEquals(UnityPipelineOffer.entries.toSet(), presentations.map { it.offer }.toSet())
    }

    private data class EditorCase(val editor: String?, val minimum: String?, val expected: Boolean)

    @Test
    fun `an Editor is older only when its major and minor numbers are below the minimum`() {
        val cases = listOf(
            EditorCase("2022.3.62f1", "6000.0", true),
            EditorCase("2023.2.20f1", "6000.0", true),
            EditorCase("6000.0.23f1", "6000.0", false),
            EditorCase("6000.1.2f1", "6000.0", false),
            EditorCase("6000.0", "6000.0", false),
            EditorCase(null, "6000.0", false),
            EditorCase("2022.3.62f1", null, false),
            EditorCase("not a version", "6000.0", false),
        )
        for ((editor, minimum, expected) in cases) {
            assertEquals(expected, unityEditorIsOlder(editor, minimum), "editor $editor, minimum $minimum")
        }
    }

    @Test
    fun `an old Editor without the package offers EDITOR_TOO_OLD, also with a held failure`() {
        assertEquals(
            UnityPipelineOffer.EDITOR_TOO_OLD,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.EDITOR_TOO_OLD,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                failure = UnityPipelineFailure.REGISTRY_UNREACHABLE,
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
    }

    @Test
    fun `an old Editor with the package shows it as installed, and offers no upgrade`() {
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.7.0-exp.1"),
                latestVersion = "0.8.0-exp.1",
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.INSTALLED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.NO_CLI,
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1"),
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
    }

    @Test
    fun `an old Editor without the Unity CLI offers EDITOR_TOO_OLD for an absent package`() {
        assertEquals(
            UnityPipelineOffer.EDITOR_TOO_OLD,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.NO_CLI,
                manifest = UnityPipelineManifest(version = null),
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
    }

    @Test
    fun `a supported Editor keeps the install and the upgrade offers`() {
        assertEquals(
            UnityPipelineOffer.INSTALL,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                editorVersion = "6000.0.23f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
        assertEquals(
            UnityPipelineOffer.UPGRADE,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.7.0-exp.1"),
                latestVersion = "0.8.0-exp.1",
                editorVersion = "6000.0.23f1",
                minimumEditorVersion = "6000.0",
            ).offer,
        )
    }

    @Test
    fun `setup adds the package only for an absent package and a supported Editor`() {
        val oldEditor = UnityPipelinePresentation(
            manifest = UnityPipelineManifest(version = null),
            editorVersion = "2022.3.62f1",
            minimumEditorVersion = "6000.0",
        )
        assertFalse(oldEditor.setupAddsPackage)
        val supportedEditor = UnityPipelinePresentation(
            manifest = UnityPipelineManifest(version = null),
            editorVersion = "6000.0.23f1",
            minimumEditorVersion = "6000.0",
        )
        assertTrue(supportedEditor.setupAddsPackage)
    }

    @Test
    fun `blockingEditor names the old Editor only when it keeps an absent package out`() {
        assertEquals(
            UnityOldEditor("6000.0", "2022.3.62f1"),
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).blockingEditor,
        )
        assertNull(
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = "0.8.0-exp.1"),
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).blockingEditor,
            "a present package is not blocked",
        )
        assertNull(
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                editorVersion = "6000.0.23f1",
                minimumEditorVersion = "6000.0",
            ).blockingEditor,
            "a supported Editor does not block",
        )
        assertNull(
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = null,
                editorVersion = "2022.3.62f1",
                minimumEditorVersion = "6000.0",
            ).blockingEditor,
            "an unreadable manifest does not block",
        )
    }

    @Test
    fun `the newest release that the Editor supports is picked`() {
        val registry = UnityPipelineRegistry(
            latest = "1.0.0",
            releases = listOf(
                UnityPipelineRelease("0.9.1", "6000.0"),
                UnityPipelineRelease("1.0.0", "6000.2"),
            ),
        )
        assertEquals(UnityPipelineRelease("0.9.1", "6000.0"), unityPipelineReleaseFor(registry, "6000.0.40f1"))
        assertEquals(UnityPipelineRelease("1.0.0", "6000.2"), unityPipelineReleaseFor(registry, "6000.2.1f1"))
        assertNull(unityPipelineReleaseFor(registry, "2022.3.62f1"), "no release supports an old Editor")
        assertEquals(
            UnityPipelineRelease("1.0.0", "6000.2"),
            unityPipelineReleaseFor(registry, null),
            "an unknown Editor supports every release, so the latest one wins",
        )
    }

    @Test
    fun `a release without a minimum Editor fits every Editor`() {
        val registry = UnityPipelineRegistry("1.0.0", listOf(UnityPipelineRelease("1.0.0", null)))
        assertEquals(UnityPipelineRelease("1.0.0", null), unityPipelineReleaseFor(registry, "2022.3.62f1"))
    }

    @Test
    fun `the lowest minimum Editor is the oldest one of the releases`() {
        val registry = UnityPipelineRegistry(
            latest = "1.0.0",
            releases = listOf(
                UnityPipelineRelease("1.0.0", "6000.2"),
                UnityPipelineRelease("0.9.1", "6000.0"),
            ),
        )
        assertEquals("6000.0", unityPipelineLowestMinimumEditor(registry))
    }

    @Test
    fun `the lowest minimum Editor is null when a release names no minimum`() {
        val registry = UnityPipelineRegistry(
            latest = "1.0.0",
            releases = listOf(
                UnityPipelineRelease("1.0.0", null),
                UnityPipelineRelease("0.9.1", "6000.0"),
            ),
        )
        assertNull(unityPipelineLowestMinimumEditor(registry))
    }
}
