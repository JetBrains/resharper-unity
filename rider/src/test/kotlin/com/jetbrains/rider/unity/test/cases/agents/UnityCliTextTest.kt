package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliInstallMethod
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliInstallDisclosure
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliNewVersionMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliPathMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliUpdateByHandMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliVersionMessage
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import java.nio.file.Path

/**
 * What the Unity CLI section of the page says, and which values it escapes.
 *
 * Five sentences of the page carry a value Rider did not author: two versions, two paths and one
 * command. Each one reaches `comment`, which builds a `DslLabel`. That is a `JEditorPane` with an
 * HTML content type, and it inserts the string raw. So an unescaped `<` either breaks the line or
 * renders a live link, because the default hyperlink action of the component opens a browser.
 *
 * The page itself is out of reach: [com.jetbrains.rider.plugins.unity.settings.agents]
 * `.UnityForAgentsConfigurable` is internal, and the suite builds no user interface. These five
 * functions are the whole text of those rows, so they carry the rule instead.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityCliTextTest {
    @Test
    fun `the installed version is escaped`() {
        val message = unityCliVersionMessage(HOSTILE_VERSION)

        assertEscaped(message, "the installed version reached the markup unescaped")
    }

    @Test
    fun `an ordinary installed version passes through untouched`() {
        val message = unityCliVersionMessage("1.0.0-beta.11")

        assertTrue(message.contains("1.0.0-beta.11"), "escaping changed a real version: '$message'")
    }

    @Test
    fun `no local copy reads as a whole phrase and leaks no null`() {
        val message = unityCliVersionMessage(null)

        assertFalse(message.contains("null"), "the version row leaked a null: '$message'")
        assertTrue(message.contains("Not installed"), "the version row lost the empty state: '$message'")
    }

    @Test
    fun `the version on offer is escaped`() {
        // The channel manifest of the CDN reports this one, behind a blank check and nothing else.
        val message = unityCliNewVersionMessage(HOSTILE_VERSION)

        assertEscaped(message, "the version on offer reached the markup unescaped")
    }

    @Test
    fun `an ordinary version on offer passes through untouched`() {
        val message = unityCliNewVersionMessage("1.0.0-beta.11")

        assertTrue(message.contains("1.0.0-beta.11"), "escaping changed a real version: '$message'")
    }

    @Test
    fun `an update with no version named leaks no null`() {
        val message = unityCliNewVersionMessage(null)

        assertFalse(message.contains("null"), "the update row leaked a null: '$message'")
    }

    @Test
    fun `the path of the local copy is escaped`() {
        // macOS and Linux both allow this in a directory name, so the user reaches it without help.
        val message = unityCliPathMessage(Path.of("/home/t/<b>games</b>/unity"), UnityCliInstallMethod.UNKNOWN)

        assertFalse(message.contains("<b>"), "the path reached the markup unescaped: '$message'")
        assertTrue(message.contains("&lt;b&gt;"), "the path was not escaped at all: '$message'")
    }

    @Test
    fun `the path names the mechanism that installed the copy`() {
        val message = unityCliPathMessage(Path.of("/opt/homebrew/bin/unity"), UnityCliInstallMethod.HOMEBREW)

        assertTrue(message.contains("/opt/homebrew/bin/unity"), "the path row lost the path: '$message'")
        assertTrue(message.contains("Homebrew"), "the path row lost the install method: '$message'")
    }

    @Test
    fun `an unknown mechanism is left unnamed`() {
        val message = unityCliPathMessage(Path.of("/opt/unity/bin/unity"), UnityCliInstallMethod.UNKNOWN)

        assertTrue(message.contains("/opt/unity/bin/unity"), "the path row lost the path: '$message'")
        assertFalse(message.contains("·"), "the path row named a mechanism it does not know: '$message'")
    }

    @Test
    fun `the disclosure states what the install does`() {
        val message = unityCliInstallDisclosure(foreignBinaryOnPath = null)

        // This sentence is the substance of the disclosure control, and the balloon carries the same.
        assertTrue(
            message.contains("installer script of Unity"),
            "the page lost the disclosure of the install: '$message'",
        )
    }

    @Test
    fun `the binary ahead on the PATH is named and escaped`() {
        val message = unityCliInstallDisclosure(Path.of("/usr/<i>local</i>/bin/unity"))

        assertFalse(message.contains("<i>"), "the PATH entry reached the markup unescaped: '$message'")
        assertTrue(message.contains("&lt;i&gt;"), "the PATH entry was not escaped at all: '$message'")
    }

    @Test
    fun `the command to run by hand is escaped`() {
        // `unityCliUpdateCommandForClipboard` refuses a command that holds `<` today, for a reason
        // of its own. This row must stay right if that list ever relaxes.
        val message = unityCliUpdateByHandMessage("apt install <pkg>")

        assertFalse(message.contains("<pkg>"), "the command reached the markup unescaped: '$message'")
        assertTrue(message.contains("&lt;pkg&gt;"), "the command was not escaped at all: '$message'")
    }

    @Test
    fun `no safe command still reads as a whole sentence`() {
        val message = unityCliUpdateByHandMessage(null)

        assertFalse(message.contains("null"), "the manual update row leaked a null: '$message'")
        assertTrue(message.trim().endsWith("."), "the manual update row is not a sentence: '$message'")
    }

    private fun assertEscaped(message: String, what: String) {
        assertFalse(message.contains("<img"), "$what: '$message'")
        assertTrue(message.contains("&lt;img"), "$what, and it was not escaped at all: '$message'")
    }

    private companion object {
        const val HOSTILE_VERSION = "1.0.0<img src=x onerror=alert(1)>"
    }
}
