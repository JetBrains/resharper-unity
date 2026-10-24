package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupDoneMessage
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsUpdateSuggestionMessage
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertFalse
import org.junit.jupiter.api.Assertions.assertTrue
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test
import org.junit.jupiter.params.ParameterizedTest
import org.junit.jupiter.params.provider.Arguments
import org.junit.jupiter.params.provider.MethodSource
import java.util.stream.Stream

/**
 * What the suggestion balloons of a project open say.
 *
 * No case builds a balloon, so none needs an application, a project or a notification group.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityAgentsBalloonTextTest {
    @Test
    fun `the update balloon of a project open names the version on offer`() {
        val message = unityAgentsUpdateSuggestionMessage(cliVersion = "1.0.0-beta.11", packageVersion = null)

        assertTrue(message.contains("1.0.0-beta.11"), "the update suggestion lost '1.0.0-beta.11': '$message'")
    }

    @Test
    fun `a version that carries markup is escaped`() {
        // Both versions are third-party: one is the output of the Unity CLI and one is the channel
        // manifest of the CDN. Both sentences reach a JEditorPane, which renders HTML.
        val hostile = "1.0.0<img src=x onerror=alert(1)>"

        val message = unityAgentsUpdateSuggestionMessage(cliVersion = hostile, packageVersion = null)

        assertFalse(message.contains("<img"), "a version reached the markup unescaped: '$message'")
        assertTrue(message.contains("&lt;img"), "the version was not escaped to '&lt;img': '$message'")
    }

    @Test
    fun `an ordinary version passes through untouched`() {
        val message = unityAgentsUpdateSuggestionMessage(cliVersion = "1.0.0-beta.11", packageVersion = null)

        assertTrue(message.contains("1.0.0-beta.11"), "escaping changed '1.0.0-beta.11': '$message'")
    }

    @Test
    fun `the update suggestion escapes a version that carries markup in each variant`() {
        val hostileCli = "1.0.0<img src=x onerror=alert(1)>"
        val hostilePackage = "2.0.0<img src=y onerror=alert(2)>"

        val cliOnly = unityAgentsUpdateSuggestionMessage(cliVersion = hostileCli, packageVersion = null)
        val packageOnly = unityAgentsUpdateSuggestionMessage(cliVersion = null, packageVersion = hostilePackage)
        val both = unityAgentsUpdateSuggestionMessage(cliVersion = hostileCli, packageVersion = hostilePackage)

        for (message in listOf(cliOnly, packageOnly, both)) {
            assertFalse(message.contains("<img"), "a version reached the markup unescaped: '$message'")
            assertTrue(message.contains("&lt;img"), "the version was not escaped to '&lt;img': '$message'")
        }

        assertTrue(both.contains("1.0.0"), "the both-variant lost the CLI version '1.0.0': '$both'")
        assertTrue(both.contains("2.0.0"), "the both-variant lost the package version '2.0.0': '$both'")
    }

    @Test
    fun `the update balloon for a CLI Rider cannot update says where the command is`() {
        val byHand = unityAgentsUpdateSuggestionMessage(cliVersion = "1.0.0-beta.11", packageVersion = null, cliByHand = true)
        val byRider = unityAgentsUpdateSuggestionMessage(cliVersion = "1.0.0-beta.11", packageVersion = null, cliByHand = false)

        assertTrue(byHand.contains("Unity Agents Setup"), "the balloon did not name 'Unity Agents Setup': '$byHand'")
        assertFalse(byRider.contains("Unity Agents Setup"), "a CLI that Rider updates got the by-hand sentence: '$byRider'")
    }

    @ParameterizedTest
    @MethodSource("setupDoneCases")
    fun `setup done message rules`(
        installedCli: Boolean,
        installedPackage: Boolean,
        installedSkill: Boolean,
        skillPresent: Boolean,
        expected: String,
    ) {
        val message = unityAgentsSetupDoneMessage(
            installedCli = installedCli,
            installedPackage = installedPackage,
            installedSkill = installedSkill,
            skillPresent = skillPresent,
        )
        assertEquals(expected, message)
    }

    companion object {
        @JvmStatic
        fun setupDoneCases(): Stream<Arguments> = Stream.of(
            Arguments.of(true, true, true, true, UnityBundle.message("unity.agents.setup.done.content.cli")),
            Arguments.of(true, true, false, true, UnityBundle.message("unity.agents.setup.done.content.cli.package")),
            Arguments.of(true, true, false, false, UnityBundle.message("unity.agents.setup.done.content.cli.package")),
            Arguments.of(true, false, true, true, UnityBundle.message("unity.agents.setup.done.content.cli.skill")),
            Arguments.of(true, false, false, true, UnityBundle.message("unity.agents.setup.done.content.cli.only")),
            Arguments.of(true, false, false, false, UnityBundle.message("unity.agents.setup.done.content.cli.only")),
            Arguments.of(false, true, true, true, UnityBundle.message("unity.agents.setup.done.content")),
            Arguments.of(false, true, false, false, UnityBundle.message("unity.agents.setup.done.content.package")),
            Arguments.of(false, false, true, true, UnityBundle.message("unity.agents.setup.done.content.skill")),
            Arguments.of(false, false, false, true, UnityBundle.message("unity.agents.setup.done.content")),
            Arguments.of(false, false, false, false, UnityBundle.message("unity.agents.setup.done.content.package")),
        )
    }
}
