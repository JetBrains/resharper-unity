package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliOperation
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliConfirmationMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliConfirmationTitle
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateSuggestionMessage
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

/**
 * What the two confirmation balloons say.
 *
 * The `when` over [UnityCliOperation] is exhaustive, so the build guards which case answers. What
 * it does not guard is the two facts below, and each one is a whole sentence the user reads.
 *
 * No case builds a balloon, so none needs an application, a project or a notification group.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityCliConfirmationTextTest {
    @Test
    fun `the install confirmation tells the user to open a terminal`() {
        val message = unityCliConfirmationMessage(UnityCliOperation.INSTALLING, version = null)

        // The installer script prints this advice, and its output reaches the log alone. The
        // balloon is the one surface that carries it to the user.
        assertTrue(message.contains("terminal"), "the install confirmation lost the terminal: '$message'")
    }

    @Test
    fun `the install confirmation names no version`() {
        val message = unityCliConfirmationMessage(UnityCliOperation.INSTALLING, version = "1.0.0-beta.10")

        // The caller passes null, and the sentence must stay right if a later one passes a version.
        assertFalse(message.contains("1.0.0"), "the install confirmation named a version: '$message'")
    }

    @Test
    fun `the update confirmation names the new version`() {
        val message = unityCliConfirmationMessage(UnityCliOperation.UPDATING, version = "1.0.0-beta.10")

        assertTrue(message.contains("1.0.0-beta.10"), "the update confirmation lost the version: '$message'")
    }

    @Test
    fun `an update with no version still reads as a whole sentence`() {
        val message = unityCliConfirmationMessage(UnityCliOperation.UPDATING, version = null)

        // Detection can come back with nothing after a command that exited 0. With the fallback
        // removed, the message reads "…to null." or "…to ." and the user sees a defect.
        assertFalse(message.contains("null"), "the update confirmation leaked a null: '$message'")
        assertTrue(message.trim().endsWith("."), "the update confirmation is not a sentence: '$message'")
    }

    @Test
    fun `the update balloon of a project open names the version on offer`() {
        val message = unityCliUpdateSuggestionMessage(latestVersion = "1.0.0-beta.11")

        assertTrue(message.contains("1.0.0-beta.11"), "the update suggestion lost the version: '$message'")
    }

    @Test
    fun `an update suggestion with no version still reads as a whole sentence`() {
        val message = unityCliUpdateSuggestionMessage(latestVersion = null)

        // Detection can come back with no latest version. With the fallback removed, the sentence
        // reads "of Unity CLI null is available" and the user sees a defect.
        assertFalse(message.contains("null"), "the update suggestion leaked a null: '$message'")
        assertTrue(message.trim().endsWith("."), "the update suggestion is not a sentence: '$message'")
    }

    @Test
    fun `the install balloon carries the disclosure of the install`() {
        val message = UnityBundle.message("unity.agents.cli.suggest.install.content")

        // The balloon installs now, so it must say what the install does. This sentence is the
        // substance of the single-entry-point control, and the page carries the same one.
        assertTrue(
            message.contains("installer script of Unity"),
            "the install balloon lost the disclosure: '$message'",
        )
    }

    @Test
    fun `a version that carries markup is escaped`() {
        // Both versions are third-party: one is the output of the Unity CLI and one is the channel
        // manifest of the CDN. Both sentences reach a JEditorPane, which renders HTML.
        val hostile = "1.0.0<img src=x onerror=alert(1)>"

        val update = unityCliConfirmationMessage(UnityCliOperation.UPDATING, version = hostile)
        val suggestion = unityCliUpdateSuggestionMessage(latestVersion = hostile)

        for (message in listOf(update, suggestion)) {
            assertFalse(message.contains("<img"), "a version reached the markup unescaped: '$message'")
            assertTrue(message.contains("&lt;img"), "the version was not escaped at all: '$message'")
        }
    }

    @Test
    fun `an ordinary version passes through untouched`() {
        val message = unityCliUpdateSuggestionMessage(latestVersion = "1.0.0-beta.11")

        assertTrue(message.contains("1.0.0-beta.11"), "escaping changed a real version: '$message'")
    }

    @Test
    fun `the two steps carry a different title`() {
        assertEquals("Unity CLI installed", unityCliConfirmationTitle(UnityCliOperation.INSTALLING))
        assertEquals("Unity CLI updated", unityCliConfirmationTitle(UnityCliOperation.UPDATING))
    }
}
