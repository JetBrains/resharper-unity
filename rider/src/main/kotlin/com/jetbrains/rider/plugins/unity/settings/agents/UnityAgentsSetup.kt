package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.icons.AllIcons
import com.intellij.ide.actions.ShowLogAction
import com.intellij.ide.trustedProjects.TrustedProjects
import com.intellij.notification.Notification
import com.intellij.notification.NotificationAction
import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.notification.impl.NotificationFullContent
import com.intellij.openapi.application.EDT
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsContexts
import com.jetbrains.rider.plugins.unity.UnityBundle
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@Service(Service.Level.PROJECT)
internal class UnityAgentsSetupService(
    private val project: Project,
    private val scope: CoroutineScope,
) {
    fun requestSetup() {
        scope.launch { runSetup() }
    }

    fun requestUpdate() {
        scope.launch { runUpdate() }
    }

    private suspend fun runSetup() {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        var installedCli = false
        var cli = readUnityCli(project)
        if (cli.location == null) {
            UnityCliStatusProvider.getInstance().install(project, ModalityState.nonModal())
            cli = readUnityCli(project)
            if (cli.location == null) {
                logger.info("The Unity CLI install did not land, so the setup chain stopped")
                return
            }
            installedCli = true
        }

        val pipelineProvider = UnityPipelineStatusProvider.getInstance(project)
        val pipeline = pipelineProvider.read(cli)
        if (pipeline.offer == UnityPipelineOffer.INSTALL) {
            pipelineProvider.install(ModalityState.nonModal())
        }

        val pipelineAfter = pipelineProvider.read(cli)
        when (pipelineAfter.offer) {
            UnityPipelineOffer.FAILED -> showPackageFailureBalloon(
                title = UnityBundle.message("unity.agents.setup.package.failed.title"),
                failure = pipelineAfter.failure ?: UnityPipelineFailure.NOT_CONFIRMED,
                failureExitCode = pipelineAfter.failureExitCode,
                displayId = PACKAGE_FAILED_DISPLAY_ID,
                retry = { requestSetup() },
            )
            UnityPipelineOffer.INSTALLED, UnityPipelineOffer.UPGRADE -> {
                val message = if (installedCli) {
                    UnityBundle.message("unity.agents.setup.done.content.cli")
                }
                else {
                    UnityBundle.message("unity.agents.setup.done.content")
                }
                showSuccessBalloon(
                    title = UnityBundle.message("unity.agents.setup.done.title"),
                    body = message,
                )
            }
            UnityPipelineOffer.GATED,
            UnityPipelineOffer.CHECKING,
            UnityPipelineOffer.UNREADABLE,
            UnityPipelineOffer.INSTALL,
            UnityPipelineOffer.BUSY,
                -> logger.info("The setup chain ended with the package offer ${pipelineAfter.offer}, so it shows no balloon")
        }
    }

    private suspend fun runUpdate() {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        var cli = readUnityCli(project)
        if (unityCliUpdatable(cli)) {
            UnityCliStatusProvider.getInstance().update(project, ModalityState.nonModal())
            cli = readUnityCli(project)
            if (cli.failure != null) {
                logger.info("The Unity CLI update failed, so the update chain stopped")
                return
            }
        }

        val provider = UnityPipelineStatusProvider.getInstance(project)
        if (provider.read(cli).offer != UnityPipelineOffer.UPGRADE) return
        provider.upgrade(ModalityState.nonModal())
        val after = provider.read(cli)
        if (after.offer == UnityPipelineOffer.FAILED) {
            showPackageFailureBalloon(
                title = UnityBundle.message("unity.agents.update.package.failed.title"),
                failure = after.failure ?: UnityPipelineFailure.NOT_CONFIRMED,
                failureExitCode = after.failureExitCode,
                displayId = PACKAGE_UPDATE_FAILED_DISPLAY_ID,
                retry = { requestUpdate() },
            )
        }
    }

    @Suppress("DialogTitleCapitalization")
    private suspend fun showPackageFailureBalloon(
        title: @NlsContexts.NotificationTitle String,
        failure: UnityPipelineFailure,
        failureExitCode: Int?,
        displayId: String,
        retry: () -> Unit,
    ) {
        val pageIsShowing = withContext(Dispatchers.EDT) {
            UnityForAgentsPageVisibility.getInstance().isAnyPageShowing()
        }
        if (pageIsShowing) return

        val notification = NotificationGroupManager.getInstance()
            .getNotificationGroup(UNITY_AGENTS_ERRORS_GROUP)
            .createNotification(
                title,
                unityPipelineFailureMessage(failure, failureExitCode),
                NotificationType.ERROR,
            )
        notification.setDisplayId(displayId)
        notification.setSuggestionType(true)
        notification.setAddExtraAction(true)
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.pipeline.try.again")) {
                UnityPipelineStatusProvider.getInstance(project).clearFailure()
                retry()
            }
        )
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.open.settings")) {
                openUnityForAgentsPage(project)
            }
        )
        if (ShowLogAction.isSupported()) {
            notification.addAction(ShowLogAction.notificationAction())
        }
        notification.notify(project)
    }

    private fun showSuccessBalloon(
        title: @NlsContexts.NotificationTitle String,
        body: @NlsContexts.NotificationContent String,
    ) {
        val notification = UnityAgentsFullContentNotification(UNITY_AGENTS_CONFIRMATIONS_GROUP, title, body)
        notification.setDisplayId(SETUP_DONE_DISPLAY_ID)
        notification.setIcon(AllIcons.Status.Success)
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.setup.show.skills")) {
                openUnityForAgentsPage(project)
            }
        )
        notification.notify(project)
    }

    companion object {
        fun getInstance(project: Project): UnityAgentsSetupService = project.service()

        private val logger: Logger = Logger.getInstance(UnityAgentsSetupService::class.java)
        private const val UNITY_AGENTS_CONFIRMATIONS_GROUP = "Unity for Agents Confirmations"
        private const val UNITY_AGENTS_ERRORS_GROUP = "Unity for Agents Errors"
        private const val SETUP_DONE_DISPLAY_ID = "unity.for.agents.setup.done"
        private const val PACKAGE_FAILED_DISPLAY_ID = "unity.for.agents.setup.package.failed"
        private const val PACKAGE_UPDATE_FAILED_DISPLAY_ID = "unity.for.agents.update.package.failed"
    }
}

internal class UnityAgentsFullContentNotification(
    groupId: String,
    title: @NlsContexts.NotificationTitle String,
    content: @NlsContexts.NotificationContent String,
) : Notification(groupId, title, content, NotificationType.INFORMATION), NotificationFullContent
