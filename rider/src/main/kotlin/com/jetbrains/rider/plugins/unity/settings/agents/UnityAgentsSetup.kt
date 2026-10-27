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
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsContexts
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

@Service(Service.Level.PROJECT)
internal class UnityAgentsSetupService(
    private val project: Project,
    private val scope: CoroutineScope,
) : UnityAgentsSetupProvider {
    private val setupLock = Any()
    private var setupJob: Job? = null

    override fun requestSetup(source: UnityAgentsSource, missingSteps: List<UnityAgentsSetupStep>): Job = startSetup(source) {
        UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.INSTALL, source, missingSteps)
    }

    private fun retrySetup(source: UnityAgentsSource) {
        startSetup(source) {
            UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.RETRY, source)
        }
    }

    private fun startSetup(source: UnityAgentsSource, reportClick: () -> Unit): Job = synchronized(setupLock) {
        setupJob?.takeIf { it.isActive } ?: run {
            reportClick()
            scope.launch { runSetup(source) }.also { setupJob = it }
        }
    }

    override suspend fun awaitSetup() {
        synchronized(setupLock) { setupJob }?.join()
    }

    override fun openSettings(source: UnityAgentsSource) {
        UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.OPEN_SETTINGS, source)
        openUnityForAgentsPage(project)
    }

    override fun reportDismiss(source: UnityAgentsSource) {
        UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.DISMISS, source)
    }

    override fun isRunning(): Boolean = synchronized(setupLock) { setupJob?.isActive == true }

    override fun isAvailable(): Boolean =
        UnityForAgentsFeature.isEnabled() &&
            !project.isDefault &&
            project.isUnityProject.value &&
            TrustedProjects.isProjectTrusted(project)

    override suspend fun readMissingSteps(): List<UnityAgentsSetupStep>? {
        if (!isAvailable()) return null

        val cli = readUnityCli(project)
        val pipeline = UnityPipelineStatusProvider.getInstance(project).read(cli)
        val skill = UnitySkillStatusProvider.getInstance(project).read(cli, pipeline)
        return unityAgentsMissingSteps(
            cliMissing = cli.location == null,
            packageMissing = pipeline.needsPipelinePackage == true,
            skillMissing = skill.offer in MISSING_SKILL_OFFERS,
        )
    }

    override fun requestUpdate(source: UnityAgentsSource) {
        UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.UPDATE, source)
        startUpdate(source)
    }

    private fun retryUpdate(source: UnityAgentsSource) {
        UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.RETRY, source)
        startUpdate(source)
    }

    private fun startUpdate(source: UnityAgentsSource) {
        scope.launch { runUpdate(source) }
    }

    @Suppress("DialogTitleCapitalization")
    private suspend fun runSetup(source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        var installedCli = false
        var cli = readUnityCli(project)
        if (cli.location == null) {
            UnityCliStatusProvider.getInstance().install(project, ModalityState.nonModal(), source)
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
            pipelineProvider.install(ModalityState.nonModal(), source)
        }

        val pipelineAfter = pipelineProvider.read(cli)
        val installedPackage = pipeline.offer == UnityPipelineOffer.INSTALL &&
            (pipelineAfter.offer == UnityPipelineOffer.INSTALLED || pipelineAfter.offer == UnityPipelineOffer.UPGRADE)
        when (pipelineAfter.offer) {
            UnityPipelineOffer.FAILED -> showFailureBalloon(
                title = UnityBundle.message("unity.agents.setup.package.failed.title"),
                content = unityPipelineFailureMessage(
                    pipelineAfter.failure ?: UnityPipelineFailure.NOT_CONFIRMED,
                    pipelineAfter.failureExitCode,
                    pipelineAfter.failureCliError,
                ),
                displayId = PACKAGE_FAILED_DISPLAY_ID,
                clearFailure = UnityPipelineStatusProvider.getInstance(project)::clearFailure,
                retry = { retrySetup(UnityAgentsSource.NOTIFICATION) },
            )

            UnityPipelineOffer.INSTALLED, UnityPipelineOffer.UPGRADE -> {
                val skillProvider = UnitySkillStatusProvider.getInstance(project)
                val skill = skillProvider.read(cli, pipelineAfter)
                val needsSkill = skill.offer == UnitySkillOffer.INSTALL || skill.offer == UnitySkillOffer.FAILED
                val finalSkill = if (needsSkill) {
                    skillProvider.install(UnitySkillTarget.ALL, ModalityState.nonModal(), source)
                    skillProvider.read(cli, pipelineAfter)
                } else {
                    skill
                }
                if (finalSkill.offer == UnitySkillOffer.FAILED) {
                    showFailureBalloon(
                        title = UnityBundle.message("unity.agents.setup.skill.failed.title"),
                        content = unityAgentsSkillFailedMessage(
                            installedCli,
                            finalSkill.failure ?: UnitySkillFailure.NOT_CONFIRMED,
                            finalSkill.failureExitCode,
                        ),
                        displayId = SKILL_FAILED_DISPLAY_ID,
                        clearFailure = skillProvider::clearFailure,
                        retry = { retrySetup(UnityAgentsSource.NOTIFICATION) },
                    )
                    return
                }
                val installedSkill = needsSkill && finalSkill.offer == UnitySkillOffer.INSTALLED
                val skillPresent = finalSkill.offer == UnitySkillOffer.INSTALLED
                showSuccessBalloon(
                    title = UnityBundle.message("unity.agents.setup.done.title"),
                    body = unityAgentsSetupDoneMessage(installedCli, installedPackage, installedSkill, skillPresent),
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

    private suspend fun runUpdate(source: UnityAgentsSource) {
        if (!UnityForAgentsFeature.isEnabled()) return
        if (!TrustedProjects.isProjectTrusted(project)) return

        var cli = readUnityCli(project)
        if (unityCliUpdatable(cli)) {
            UnityCliStatusProvider.getInstance().update(project, ModalityState.nonModal(), source)
            cli = readUnityCli(project)
            if (cli.failure != null) {
                logger.info("The Unity CLI update failed, so the update chain stopped")
                return
            }
        }

        val provider = UnityPipelineStatusProvider.getInstance(project)
        if (provider.read(cli).offer != UnityPipelineOffer.UPGRADE) return
        provider.upgrade(ModalityState.nonModal(), source)
        val after = provider.read(cli)
        if (after.offer == UnityPipelineOffer.FAILED) {
            showFailureBalloon(
                title = UnityBundle.message("unity.agents.update.package.failed.title"),
                content = unityPipelineFailureMessage(
                    after.failure ?: UnityPipelineFailure.NOT_CONFIRMED,
                    after.failureExitCode,
                    after.failureCliError,
                ),
                displayId = PACKAGE_UPDATE_FAILED_DISPLAY_ID,
                clearFailure = UnityPipelineStatusProvider.getInstance(project)::clearFailure,
                retry = { retryUpdate(UnityAgentsSource.NOTIFICATION) },
            )
        }
    }

    @Suppress("DialogTitleCapitalization")
    private suspend fun showFailureBalloon(
        title: @NlsContexts.NotificationTitle String,
        content: @NlsContexts.NotificationContent String,
        displayId: String,
        clearFailure: () -> Unit,
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
                content,
                NotificationType.ERROR,
            )
        notification.setDisplayId(displayId)
        notification.setSuggestionType(true)
        notification.setAddExtraAction(true)
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.pipeline.try.again")) {
                clearFailure()
                retry()
            }
        )
        notification.addAction(
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.open.settings")) {
                openSettings(UnityAgentsSource.NOTIFICATION)
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
            NotificationAction.createSimpleExpiring(UnityBundle.message("unity.agents.skills.browse")) {
                UnityForAgentsUsagesCollector.logActionClicked(project, UnityAgentsAction.BROWSE_SKILLS, UnityAgentsSource.NOTIFICATION)
                openUnitySkillsPage(project)
            }
        )
        notification.notify(project)
    }

    companion object {
        private val logger: Logger = Logger.getInstance(UnityAgentsSetupService::class.java)
        private const val UNITY_AGENTS_CONFIRMATIONS_GROUP = "Unity for Agents Confirmations"
        private const val UNITY_AGENTS_ERRORS_GROUP = "Unity for Agents Errors"
        private const val SETUP_DONE_DISPLAY_ID = "unity.for.agents.setup.done"
        private const val PACKAGE_FAILED_DISPLAY_ID = "unity.for.agents.setup.package.failed"
        private const val PACKAGE_UPDATE_FAILED_DISPLAY_ID = "unity.for.agents.update.package.failed"
        private const val SKILL_FAILED_DISPLAY_ID = "unity.for.agents.setup.skill.failed"

        private val MISSING_SKILL_OFFERS: Set<UnitySkillOffer> =
            setOf(UnitySkillOffer.INSTALL, UnitySkillOffer.FAILED, UnitySkillOffer.GATED)
    }
}

internal class UnityAgentsFullContentNotification(
    groupId: String,
    title: @NlsContexts.NotificationTitle String,
    content: @NlsContexts.NotificationContent String,
) : Notification(groupId, title, content, NotificationType.INFORMATION), NotificationFullContent
