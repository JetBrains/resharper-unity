package com.jetbrains.rider.unity.test.cases.agents

import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupRowLink
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupRowPresentation
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupStep
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupRowPresentation
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Assertions.assertNull
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

/** What the Unity row of the AI Agent Setup widget shows for each state of the setup. */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityAgentsSetupRowPresentationTest {
    @Test
    fun `one missing step offers Install`() {
        val presentation = presentation(missing = listOf(UnityAgentsSetupStep.CLI_SKILL))

        assertEquals("Missing: unity-cli skill", presentation.summary)
        assertEquals(listOf(UnityAgentsSetupRowLink.INSTALL, UnityAgentsSetupRowLink.DISMISS), presentation.links)
        assertEquals(listOf("Install", "Dismiss"), presentation.links.map { it.text })
        assertNull(presentation.progress)
    }

    @Test
    fun `two or more missing steps offer Install All`() {
        val presentation = presentation(missing = UnityAgentsSetupStep.entries)

        assertEquals("Missing: Unity CLI, Pipeline package, unity-cli skill", presentation.summary)
        assertEquals(listOf(UnityAgentsSetupRowLink.INSTALL_ALL, UnityAgentsSetupRowLink.DISMISS), presentation.links)
        assertEquals(listOf("Install All", "Dismiss"), presentation.links.map { it.text })
    }

    @Test
    fun `a dismissed row keeps the missing text and drops the links`() {
        val presentation = presentation(missing = listOf(UnityAgentsSetupStep.PIPELINE_PACKAGE), dismissed = true)

        assertEquals("Missing: Pipeline package", presentation.summary)
        assertEquals(emptyList<UnityAgentsSetupRowLink>(), presentation.links)
        assertNull(presentation.progress)
    }

    @Test
    fun `a running setup replaces the links with the progress text`() {
        val presentation = presentation(missing = UnityAgentsSetupStep.entries, installing = true)

        assertEquals(emptyList<UnityAgentsSetupRowLink>(), presentation.links)
        assertEquals("Installing…", presentation.progress)
    }

    @Test
    fun `a running setup shows the progress text on a dismissed row`() {
        val presentation = presentation(missing = UnityAgentsSetupStep.entries, dismissed = true, installing = true)

        assertEquals(emptyList<UnityAgentsSetupRowLink>(), presentation.links)
        assertEquals("Installing…", presentation.progress)
    }

    @Test
    fun `a running setup with nothing missing shows the complete setup`() {
        val presentation = presentation(installing = true)

        assertEquals("Enabled, 1 skill enabled", presentation.summary)
        assertNull(presentation.progress)
    }

    @Test
    fun `a complete setup counts the Unity skills`() {
        assertEquals("Enabled", presentation(enabledSkillCount = 0).summary)
        assertEquals("Enabled, 1 skill enabled", presentation(enabledSkillCount = 1).summary)
        assertEquals("Enabled, 3 skills enabled", presentation(enabledSkillCount = 3).summary)
        assertEquals(emptyList<UnityAgentsSetupRowLink>(), presentation(enabledSkillCount = 3).links)
    }

    private fun presentation(
        missing: List<UnityAgentsSetupStep> = emptyList(),
        dismissed: Boolean = false,
        installing: Boolean = false,
        enabledSkillCount: Int = 1,
    ): UnityAgentsSetupRowPresentation = unityAgentsSetupRowPresentation(missing, dismissed, installing, enabledSkillCount)
}
