package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.notification.NotificationAction
import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.openapi.help.HelpManager
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.openapi.util.NlsContexts
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import kotlinx.coroutines.flow.first

class UnityCliSuggestionActivity : ProjectActivity, DumbAware {
    override suspend fun execute(project: Project) {
        if (!UnityForAgentsFeature.isCliSuggestionEnabled()) return
        if (project.isDefault) return

        SolutionLifecycleHost.getInstance(project).isBackendLoadedFlow.first { it }
        if (!project.isUnityProject.value) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        val presentation = UnityCliStatusProvider.getInstance().read()
        val suggestion = unityCliSuggestion(
            presentation = presentation,
            installDeclined = UnityCliDeclines.isDeclined(project, UnityCliDeclines.INSTALL_DISPLAY_ID),
            updateDeclined = UnityCliDeclines.isDeclined(project, UnityCliDeclines.UPDATE_DISPLAY_ID),
        )
        if (suggestion == UnityCliSuggestion.NONE) return

        logger.debug("The Unity CLI sweep offers $suggestion for ${project.name}")
        prompt(project, suggestion, presentation)
    }

    @Suppress("DialogTitleCapitalization")
    private fun prompt(
        project: Project,
        suggestion: UnityCliSuggestion,
        presentation: UnityCliPresentation,
    ) {
        val displayId = UnityCliDeclines.displayIdOf(suggestion) ?: return
        val title = when (suggestion) {
            UnityCliSuggestion.INSTALL -> UnityBundle.message("unity.agents.cli.suggest.install.title")
            UnityCliSuggestion.UPDATE -> UnityBundle.message("unity.agents.cli.suggest.update.title")
            UnityCliSuggestion.NONE -> return
        }
        val message = when (suggestion) {
            UnityCliSuggestion.INSTALL -> UnityBundle.message("unity.agents.cli.suggest.install.content")
            UnityCliSuggestion.UPDATE -> unityCliUpdateSuggestionMessage(presentation.latestVersion)
            UnityCliSuggestion.NONE -> return
        }
        val acts = unityCliSuggestionActs(suggestion, presentation)

        val notification = NotificationGroupManager.getInstance()
            .getNotificationGroup(UNITY_AGENTS_GROUP)
            .createNotification(title, message, NotificationType.INFORMATION)
        notification.setDisplayId(displayId)
        notification.setSuggestionType(true)
        notification.addAction(
            NotificationAction.createSimpleExpiring(confirmLabel(suggestion, acts)) {
                if (!acts) {
                    openUnityForAgentsPage(project)
                }
                else {
                    val provider = UnityCliStatusProvider.getInstance()
                    when (suggestion) {
                        UnityCliSuggestion.INSTALL -> provider.requestInstall(project, ModalityState.nonModal())
                        UnityCliSuggestion.UPDATE -> provider.requestUpdate(project, ModalityState.nonModal())
                        UnityCliSuggestion.NONE -> Unit
                    }
                }
            }
        )
        notification.addAction(
            NotificationAction.createSimple(UnityBundle.message("unity.agents.learn.more")) {
                HelpManager.getInstance().invokeHelp(UNITY_FOR_AGENTS_HELP_TOPIC)
            }
        )
        notification.notify(project)
    }

    @Suppress("DialogTitleCapitalization")
    @NlsContexts.Button
    private fun confirmLabel(suggestion: UnityCliSuggestion, acts: Boolean): String = when {
        !acts -> UnityBundle.message("unity.agents.open.settings")
        suggestion == UnityCliSuggestion.UPDATE -> UnityBundle.message("unity.agents.cli.action.update")
        else -> UnityBundle.message("unity.agents.cli.action.install")
    }

    private companion object {
        val logger: Logger = Logger.getInstance(UnityCliSuggestionActivity::class.java)
        const val UNITY_AGENTS_GROUP = "Unity for Agents"
    }
}
