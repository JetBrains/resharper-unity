package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineManifest
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOffer
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOperation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelinePresentation
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineHasNewerVersion
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
 * Pins the offer ladder of the Unity Pipeline Package section. The class runs no process and reads no file.
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
                failure = UnityPipelineFailure.LAUNCH_FAILED,
            ).offer,
            "an active operation takes precedence over failure",
        )
        assertEquals(
            UnityPipelineOffer.GATED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.NO_CLI,
                failure = UnityPipelineFailure.LAUNCH_FAILED,
            ).offer,
            "a closed gate takes precedence over a failure, because a retry cannot run",
        )
        assertEquals(
            UnityPipelineOffer.FAILED,
            UnityPipelinePresentation(
                gate = UnityMcpSectionState.READY,
                manifest = UnityPipelineManifest(version = null),
                failure = UnityPipelineFailure.LAUNCH_FAILED,
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
            UnityPipelinePresentation(failure = UnityPipelineFailure.LAUNCH_FAILED),
            UnityPipelinePresentation(gate = UnityMcpSectionState.CHECKING),
            UnityPipelinePresentation(gate = UnityMcpSectionState.NO_PROJECT),
            UnityPipelinePresentation(gate = UnityMcpSectionState.READY, manifest = null),
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
}
