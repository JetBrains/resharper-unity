package com.jetbrains.rider.plugins.unity.agents

import com.intellij.ide.util.PropertiesComponent
import com.intellij.mcpserver.skills.AgentSkillCatalog
import com.intellij.mcpserver.skills.SkillWorkspaceContext
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.appender.mcpServer.agentSetup.AgentSetupRow
import com.jetbrains.rider.plugins.appender.mcpServer.agentSetup.AgentSetupRowLink
import com.jetbrains.rider.plugins.appender.mcpServer.agentSetup.AgentSetupRowProvider
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_CLI_SKILL_NAME
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_SKILL_REGISTRY_SOURCE
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupProvider
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupRowLink
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSetupStep
import com.jetbrains.rider.plugins.unity.settings.agents.UnityAgentsSource
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupRowInstallingText
import com.jetbrains.rider.plugins.unity.settings.agents.unityAgentsSetupRowPresentation
import org.jetbrains.annotations.Nls
import java.nio.file.Path

private const val DISMISSED_STEPS_KEY = "rider.unity.agents.setup.widget.dismissed"

internal class UnityAgentSetupRowProvider : AgentSetupRowProvider {
    override fun getTitle(): String = UnityBundle.message("unity.agents.setup.row.title")

    override fun isAvailable(project: Project): Boolean = UnityAgentsSetupProvider.getInstance(project).isAvailable()

    override suspend fun read(project: Project): AgentSetupRow? {
        val setup = UnityAgentsSetupProvider.getInstance(project)
        val status = setup.readStatus() ?: return null
        val steps = status.missing
        // A setup that an old Editor stops is not complete, so the dismissed steps stay stored.
        if (steps.isEmpty() && status.blockingEditor == null) PropertiesComponent.getInstance(project).setList(DISMISSED_STEPS_KEY, null)
        val presentation = unityAgentsSetupRowPresentation(
            missing = steps,
            dismissed = isUnityAgentsSetupDismissed(dismissedSteps(project), steps),
            installing = setup.isRunning(),
            enabledSkillCount = if (steps.isEmpty() && status.blockingEditor == null) countUnitySkills(project) else 0,
            blockingEditor = status.blockingEditor,
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

    /** The Unity CLI skill and the enabled skills of the Unity registry. Without a skill catalog or a project base path, only the CLI skill counts. */
    private suspend fun countUnitySkills(project: Project): Int {
        val context = project.basePath?.let { SkillWorkspaceContext.create(Path.of(it), project) } ?: return 1
        val registrySkills = AgentSkillCatalog.getInstance()?.enabledSkills(context).orEmpty().count { skill ->
            skill.directory.fileName?.toString() != UNITY_CLI_SKILL_NAME && UNITY_SKILL_REGISTRY_SOURCE in skill.registrySources
        }
        return 1 + registrySkills
    }
}
