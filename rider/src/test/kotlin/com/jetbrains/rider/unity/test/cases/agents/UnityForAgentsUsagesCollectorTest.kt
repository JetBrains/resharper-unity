package com.jetbrains.rider.unity.test.cases.agents

import com.intellij.internal.statistic.eventLog.events.scheme.EventsSchemeBuilder
import com.intellij.internal.statistic.eventLog.events.scheme.GroupDescriptor
import com.intellij.internal.statistic.eventLog.events.scheme.PluginSchemeDescriptor
import com.jetbrains.rider.plugins.unity.settings.agents.UnityForAgentsUsagesCollector
import com.jetbrains.rider.test.annotations.Subsystem
import com.jetbrains.rider.test.annotations.report.Feature
import com.jetbrains.rider.test.reporting.SubsystemConstants
import com.jetbrains.rider.test.shared.constants.TeamCityTags
import org.junit.jupiter.api.Assertions.assertEquals
import org.junit.jupiter.api.Tag
import org.junit.jupiter.api.Test

/**
 * The scheme of the `dotnet.unity.agents` group: the event ids, the field names and the values that each field accepts.
 *
 * The scheme is a contract with the statistics metadata server, so a rename has to fail here and not on the dashboard.
 * The test builds the scheme with [EventsSchemeBuilder], which the build also uses to register the group.
 *
 * The test reads the scheme and logs nothing. `FUCollectorTestCase` records logged events, but the test plugin of this
 * module does not load `intellij.platform.statistics.testFramework`.
 */
@Subsystem(SubsystemConstants.UNITY_PLUGIN)
@Feature("Unity for Agents")
@Tag(TeamCityTags.Plugins.Unity.General)
class UnityForAgentsUsagesCollectorTest {
    @Test
    fun `the group keeps its id and its version`() {
        val group = buildScheme()

        assertEquals("dotnet.unity.agents", group.id)
        assertEquals(1, group.version)
    }

    @Test
    fun `every event reports the fields and the values of the contract`() {
        val source = setOf("{enum:NOTIFICATION|SETTINGS|AGENT_WIDGET}")
        val result = setOf("{enum:SUCCESS|FAILED}")
        val expected = mapOf(
            "action_clicked" to mapOf(
                "action" to setOf("{enum:INSTALL|UPDATE|RETRY|DISMISS|OPEN_SETTINGS|BROWSE_SKILLS|LEARN_MORE}"),
                "source" to source,
                "missing_components" to setOf("{enum:CLI|PIPELINE_PACKAGE|CLI_SKILL}"),
            ),
            "component_operation_finished" to mapOf(
                "component" to setOf("{enum:CLI|PIPELINE_PACKAGE|CLI_SKILL}"),
                "operation" to setOf("{enum:INSTALLING|UPDATING}"),
                "source" to source,
                "result" to result,
            ),
            "mcp_operation_finished" to mapOf(
                "agent" to setOf("{enum:CODEX_CLI|CLAUDE_CODE_CLI|COPILOT_CLI|JUNIE_CLI}"),
                "operation" to setOf("{enum:CONFIGURING|REMOVING}"),
                "result" to result,
            ),
        )

        val actual = buildScheme().schema.associate { event ->
            event.event to event.fields.associate { it.path to it.value }
        }

        assertEquals(expected, actual)
    }

    @Test
    fun `every field carries a description for the scheme`() {
        val fieldsWithoutDescription = buildScheme().schema.flatMap { event ->
            event.fields.filter { it.description.isNullOrBlank() }.map { "${event.event}.${it.path}" }
        }

        assertEquals(emptyList<String>(), fieldsWithoutDescription)
    }

    private fun buildScheme(): GroupDescriptor {
        val collector = EventsSchemeBuilder.FeatureUsageCollectorInfo(UnityForAgentsUsagesCollector, PluginSchemeDescriptor("test"))
        return EventsSchemeBuilder.collectGroupsFromExtensions("counter", listOf(collector), recorder = null).single()
    }
}
