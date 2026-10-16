package com.jetbrains.rider.plugins.unity.agents

import com.intellij.aiagentextensions.skills.loader.github.GitHubSkillSourceTracker
import com.intellij.aiagentextensions.skills.settings.EnabledAgentSkillsCounter
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.appender.mcpServer.agentSetup.AgentSetupRow
import com.jetbrains.rider.plugins.appender.mcpServer.agentSetup.AgentSetupRowLink
import com.jetbrains.rider.plugins.appender.mcpServer.agentSetup.AgentSetupRowProvider
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_SKILL_NAME
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupProvider
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupRowLink
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupStep
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSource
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupRowInstallingText
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupRowPresentation
import org.jetbrains.annotations.Nls

private const val DISMISSED_STEPS_KEY = "rider.unity.agents.setup.widget.dismissed"

internal class UnityAgentSetupRowProvider : AgentSetupRowProvider {
    override fun getTitle(): String = UnityBundle.message("unity.agents.setup.row.title")

    override fun isAvailable(project: Project): Boolean = UnityAgentsSetupProvider.getInstance(project).isAvailable()

    override suspend fun read(project: Project): AgentSetupRow? {
        val setup = UnityAgentsSetupProvider.getInstance(project)
        val steps = setup.readMissingSteps() ?: return null
        if (steps.isEmpty()) PropertiesComponent.getInstance(project).setList(DISMISSED_STEPS_KEY, null)
        val presentation = unityAgentsSetupRowPresentation(
            missing = steps,
            dismissed = isUnityAgentsSetupDismissed(dismissedSteps(project), steps),
            installing = setup.isRunning(),
            enabledSkillCount = if (steps.isEmpty()) countUnitySkills(project) else 0,
        )
        return AgentSetupRow(
            summary = presentation.summary,
            links = presentation.links.map { rowLink(project, it, steps, presentation.summary) },
            progress = presentation.progress,
        )
    }

    override fun open(project: Project) {
        UnityAgentsSetupProvider.getInstance(project).openSettings(UnityAgentsSource.AGENT_WIDGET)
    }

    override suspend fun awaitIdle(project: Project) {
        UnityAgentsSetupProvider.getInstance(project).awaitSetup()
    }

    private fun isUnityAgentsSetupDismissed(dismissed: Set<UnityAgentsSetupStep>, missing: List<UnityAgentsSetupStep>): Boolean =
        dismissed.isNotEmpty() && dismissed == missing.toSet()

    private fun rowLink(
        project: Project,
        link: UnityAgentsSetupRowLink,
        missing: List<UnityAgentsSetupStep>,
        summary: @Nls String,
    ): AgentSetupRowLink =
        when (link) {
            UnityAgentsSetupRowLink.INSTALL, UnityAgentsSetupRowLink.INSTALL_ALL -> AgentSetupRowLink(
                text = link.text,
                progress = unityAgentsSetupRowInstallingText(),
            ) {
                UnityAgentsSetupProvider.getInstance(project).requestSetup(UnityAgentsSource.AGENT_WIDGET, missing).join()
                null
            }

            UnityAgentsSetupRowLink.DISMISS -> AgentSetupRowLink(text = link.text) {
                UnityAgentsSetupProvider.getInstance(project).reportDismiss(UnityAgentsSource.AGENT_WIDGET)
                PropertiesComponent.getInstance(project).setList(DISMISSED_STEPS_KEY, missing.map { it.name })
                AgentSetupRow(summary = summary)
            }
        }

    private fun dismissedSteps(project: Project): Set<UnityAgentsSetupStep> =
        PropertiesComponent.getInstance(project).getList(DISMISSED_STEPS_KEY).orEmpty()
            .mapNotNullTo(mutableSetOf()) { name -> UnityAgentsSetupStep.entries.firstOrNull { it.name == name } }

    private fun isUnityRegistrySkill(folderName: String?, source: String?): Boolean =
        folderName != UNITY_CLI_SKILL_NAME && source == UNITY_SKILL_REGISTRY_SOURCE

    private suspend fun countUnitySkills(project: Project): Int = 1 + EnabledAgentSkillsCounter.countEnabledSkills(project) { skill ->
        val directory = skill.localDirectoryPath
        isUnityRegistrySkill(directory?.fileName?.toString(), directory?.let(GitHubSkillSourceTracker::readSource))
    }
}
