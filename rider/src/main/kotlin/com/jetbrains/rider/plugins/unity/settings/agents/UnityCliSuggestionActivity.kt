package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.notification.NotificationAction
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.help.HelpManager
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import kotlinx.coroutines.flow.first
import org.jetbrains.annotations.ApiStatus

@ApiStatus.Internal
class UnityCliSuggestionActivity : ProjectActivity, DumbAware {
    override suspend fun execute(project: Project) {
        if (!UnityForAgentsFeature.isCliSuggestionEnabled()) return
        if (project.isDefault) return

        SolutionLifecycleHost.getInstance(project).isBackendLoadedFlow.first { it }
        if (!project.isUnityProject.value) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        val presentation = UnityCliStatusProvider.getInstance().read()
        val pipeline = UnityPipelineStatusProvider.getInstance(project).read(presentation)
        val skill = UnitySkillStatusProvider.getInstance(project).read(presentation, pipeline)
        val packageMissing = pipeline.offer == UnityPipelineOffer.INSTALL
        val skillMissing = skill.installs?.isEmpty() == true &&
                           (packageMissing || skill.offer == UnitySkillOffer.INSTALL || skill.offer == UnitySkillOffer.FAILED)
        val facts = UnityAgentsSweepFacts(
            packageMissing = packageMissing,
            packageUpgradable = pipeline.offer == UnityPipelineOffer.UPGRADE,
            installDeclined = UnityCliDeclines.isDeclined(project, UnityCliDeclines.INSTALL_DISPLAY_ID),
            updateDeclined = UnityCliDeclines.isDeclined(project, UnityCliDeclines.UPDATE_DISPLAY_ID),
            skillMissing = skillMissing,
        )
        val suggestion = unityCliSuggestion(presentation, facts)
        if (suggestion == UnityCliSuggestion.NONE) return

        logger.debug("The Unity CLI sweep offers $suggestion for ${project.name}")
        prompt(project, suggestion, presentation, pipeline, skillMissing)
    }

    @Suppress("DialogTitleCapitalization")
    private fun prompt(
        project: Project,
        suggestion: UnityCliSuggestion,
        presentation: UnityCliPresentation,
        pipeline: UnityPipelinePresentation,
        skillMissing: Boolean,
    ) {
        val displayId = UnityCliDeclines.displayIdOf(suggestion) ?: return
        when (suggestion) {
            UnityCliSuggestion.INSTALL -> {
                val cliMissing = presentation.copy(projectTrusted = true).offer == UnityCliOffer.INSTALL
                val packageMissing = pipeline.offer == UnityPipelineOffer.INSTALL
                val missingSteps = unityAgentsMissingSteps(cliMissing, packageMissing, skillMissing)
                val (title, message) = unityAgentsSetupPrompt(cliMissing, packageMissing, skillMissing)
                val notification = UnityAgentsFullContentNotification(
                    UNITY_AGENTS_GROUP,
                    title,
                    message,
                )
                notification.setDisplayId(displayId)
                notification.setSuggestionType(true)
                notification.addAction(
                    NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.setup.action.install")) {
                        UnityAgentsSetupProvider.getInstance(project).requestSetup(UnityAgentsSource.NOTIFICATION, missingSteps)
                    }
                )
                notification.addAction(
                    NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.setup.action.manual")) {
                        UnityAgentsSetupProvider.getInstance(project).openSettings(UnityAgentsSource.NOTIFICATION)
                    }
                )
                notification.notify(project)
            }

            UnityCliSuggestion.UPDATE -> {
                val packageUpgradable = pipeline.offer == UnityPipelineOffer.UPGRADE
                val acts = unityCliSuggestionActs(suggestion, presentation, packageUpgradable)
                val cliOffer = presentation.copy(projectTrusted = true).offer
                val cliVersion =
                    if (cliOffer == UnityCliOffer.UPDATE || cliOffer == UnityCliOffer.SHOW_UPDATE_COMMAND) {
                        presentation.latestVersion
                    }
                    else {
                        null
                    }
                val packageVersion = if (packageUpgradable) pipeline.latestVersion else null
                val notification = UnityAgentsFullContentNotification(
                    UNITY_AGENTS_GROUP,
                    UnityBundle.message("unity.agents.update.suggest.title"),
                    unityAgentsUpdateSuggestionMessage(
                        cliVersion,
                        packageVersion,
                        cliByHand = cliOffer == UnityCliOffer.SHOW_UPDATE_COMMAND,
                    ),
                )
                val actions = unityAgentsUpdateActions(acts, cliOffer)
                notification.setDisplayId(displayId)
                notification.setSuggestionType(actions.first() == UnityAgentsUpdateAction.UPDATE)
                notification.setAddExtraAction(true)
                for (action in actions) {
                    notification.addAction(
                        when (action) {
                            UnityAgentsUpdateAction.UPDATE ->
                                NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.update.action")) {
                                    UnityAgentsSetupProvider.getInstance(project).requestUpdate(UnityAgentsSource.NOTIFICATION)
                                }
                            UnityAgentsUpdateAction.SHOW_UPDATE_COMMAND ->
                                NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.update.show.command")) {
                                    UnityAgentsSetupProvider.getInstance(project).openSettings(UnityAgentsSource.NOTIFICATION)
                                }
                            UnityAgentsUpdateAction.LEARN_MORE ->
                                NotificationAction.createSimple(UnityBundle.message("unity.agents.learn.more")) {
                                    UnityForAgentsUsagesCollector.logActionClicked(
                                        project,
                                        UnityAgentsAction.LEARN_MORE,
                                        UnityAgentsSource.NOTIFICATION,
                                    )
                                    HelpManager.getInstance().invokeHelp(UNITY_FOR_AGENTS_HELP_TOPIC)
                                }
                        }
                    )
                }
                notification.notify(project)
            }

            UnityCliSuggestion.NONE -> Unit
        }
    }

    private companion object {
        val logger: Logger = Logger.getInstance(UnityCliSuggestionActivity::class.java)
        const val UNITY_AGENTS_GROUP = "Unity for Agents"
    }
}
