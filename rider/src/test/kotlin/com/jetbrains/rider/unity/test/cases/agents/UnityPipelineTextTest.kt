package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityMcpSectionState
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineFailure
import com.jetbrains.rider.plugins.unity.settings.agents.UnityPipelineOperation
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineBusyMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineFailureMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineGateMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineInstallHint
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineNewVersionMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityPipelineVersionMessage
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertNotNull
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

/**
 * Pins the texts of the Unity Pipeline Package section.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityPipelineTextTest {
    @Test
    fun `the install hint escapes a hostile version and passes an ordinary version through untouched`() {
        val hostileMessage = unityPipelineInstallHint(HOSTILE_VERSION)
        assertEscaped(hostileMessage, "the install hint reached the markup unescaped")

        val ordinaryVersion = "0.8.0-exp.1"
        val ordinaryMessage = unityPipelineInstallHint(ordinaryVersion)
        assertTrue(ordinaryMessage.contains(ordinaryVersion), "escaping changed '$ordinaryVersion': '$ordinaryMessage'")
    }

    @Test
    fun `the install hint with no version contains no null and no placeholder`() {
        val message = unityPipelineInstallHint(null)
        assertFalse(message.contains("null"), "the install hint leaked a null: '$message'")
        assertFalse(message.contains("{0}"), "the install hint leaked a placeholder: '$message'")
    }

    @Test
    fun `the version message and the new-version message escape a hostile version`() {
        val versionMessage = unityPipelineVersionMessage(HOSTILE_VERSION)
        assertEscaped(versionMessage, "the version message reached the markup unescaped")

        val newVersionMessage = unityPipelineNewVersionMessage(HOSTILE_VERSION)
        assertEscaped(newVersionMessage, "the new-version message reached the markup unescaped")
    }

    @Test
    fun `the gate message is not null for closed states and is null for checking and ready`() {
        assertNotNull(unityPipelineGateMessage(UnityMcpSectionState.NO_PROJECT))
        assertNotNull(unityPipelineGateMessage(UnityMcpSectionState.NOT_TRUSTED))
        assertNotNull(unityPipelineGateMessage(UnityMcpSectionState.NO_CLI))
        assertNull(unityPipelineGateMessage(UnityMcpSectionState.CHECKING))
        assertNull(unityPipelineGateMessage(UnityMcpSectionState.READY))
    }

    @Test
    fun `the exit-code failure contains the exit code and every failure value has a non-blank message without placeholders`() {
        val exitCodeMessage = unityPipelineFailureMessage(UnityPipelineFailure.EXIT_CODE, 6, null)
        assertTrue(exitCodeMessage.contains("6"), "exit code 6 was not formatted: '$exitCodeMessage'")

        for (failure in UnityPipelineFailure.entries) {
            val message = unityPipelineFailureMessage(failure, 0, null)
            assertTrue(message.isNotBlank(), "message for $failure was blank")
            assertFalse(message.contains("{0}"), "message for $failure leaked placeholder: '$message'")

            val messageWithoutCode = unityPipelineFailureMessage(failure, null, null)
            assertTrue(messageWithoutCode.isNotBlank(), "message for $failure with null exitCode was blank")
            assertFalse(
                messageWithoutCode.contains("{0}"),
                "message for $failure with null exitCode leaked placeholder: '$messageWithoutCode'",
            )
        }
    }

    @Test
    fun `the error message of the Unity CLI replaces the failure text and is escaped`() {
        val cliError = "No such host is known. (packages.unity.com:443)"
        for (failure in UnityPipelineFailure.entries) {
            val message = unityPipelineFailureMessage(failure, 6, cliError)
            assertTrue(message.contains(cliError), "message for $failure lost the error of the Unity CLI: '$message'")
        }
        assertEscaped(
            unityPipelineFailureMessage(UnityPipelineFailure.EXIT_CODE, 6, HOSTILE_VERSION),
            "the error of the Unity CLI reached the markup unescaped",
        )
    }

    @Test
    fun `a long error message of the Unity CLI is cut`() {
        val message = unityPipelineFailureMessage(UnityPipelineFailure.EXIT_CODE, 6, "x".repeat(10_000))
        assertTrue(message.length < 1_000, "the message kept ${message.length} characters")
    }

    @Test
    fun `every unity pipeline operation value has a non-blank busy message`() {
        for (operation in UnityPipelineOperation.entries) {
            val message = unityPipelineBusyMessage(operation)
            assertTrue(message.isNotBlank(), "busy message for $operation was blank")
        }
    }

    private fun assertEscaped(message: String, what: String) {
        assertFalse(message.contains("<img"), "$what: '$message'")
        assertTrue(message.contains("&lt;img"), "$what, and it was not escaped to '&lt;img': '$message'")
    }

    private companion object {
        const val HOSTILE_VERSION = "1.0.0<img src=x onerror=alert(1)>"
    }
}
