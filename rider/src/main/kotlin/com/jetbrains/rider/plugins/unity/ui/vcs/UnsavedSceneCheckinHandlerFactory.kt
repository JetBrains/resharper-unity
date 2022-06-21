package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.CommitExecutor
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.openapi.vcs.ui.RefreshableOnComponent
import com.intellij.util.PairConsumer
import com.jetbrains.rd.framework.impl.RpcTimeouts
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution
import java.util.concurrent.CancellationException

/**
 * Checks if there are unsaved scenes in Unity.
 */
class UnsavedSceneCheckinHandlerFactory : CheckinHandlerFactory() {
    override fun createHandler(panel: CheckinProjectPanel, commitContext: CommitContext): CheckinHandler =
        UnresolvedMergeCheckHandler(panel)
}

private val logger = Logger.getInstance(UnsavedSceneCheckinHandlerFactory::class.java)

private class UnresolvedMergeCheckHandler(
    private val panel: CheckinProjectPanel
) : CheckinHandler() {

    private val project = panel.project
    private val settings = UnsavedCheckinState.getService(project)

    override fun getBeforeCheckinConfigurationPanel(): RefreshableOnComponent? {
        if (!project.isUnityProject() && !project.isDefault)
            return null

        return BooleanCommitOption(
            panel, UnityUIBundle.message("commitOption.check.unsaved.unity.state"), false,
            settings::checkUnsavedState
        )
    }

    override fun beforeCheckin(
        executor: CommitExecutor?,
        additionalDataConsumer: PairConsumer<Any, Any>
    ): ReturnResult {
        if (settings.checkUnsavedState && project.isUnityProject()) {
            var providerResult = false
            try {
                providerResult = project.solution.frontendBackendModel.hasUnsavedState
                    .sync(Unit, RpcTimeouts(200L, 200L))
            }
            catch (t: CancellationException){
                logger.info("Unable to fetch hasUnsavedScenes", t)
            }
            catch (t: Throwable) {
                logger.warn("Unable to fetch hasUnsavedScenes", t)
            }

            if (providerResult) return askUser()
        }

        return ReturnResult.COMMIT
    }

    private fun askUser(): ReturnResult {
        val dialogResult = Messages.showOkCancelDialog(
            project,
            UnityUIBundle.message("dialog.unsaved.message.changes.in.unity.state.will.not.be.included.in.commit"),
            UnityUIBundle.message("dialog.unsaved.title.unity.state"),
            UnityUIBundle.message("dialog.unsaved.button.commit.anyway"),
            UnityUIBundle.message("dialog.unsaved.button.cancel"),
            Messages.getWarningIcon()
        )

        if (dialogResult == Messages.OK) return ReturnResult.COMMIT
        return ReturnResult.CANCEL
    }
}
