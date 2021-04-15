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
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

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
    private val settings = UnityCheckinState.getService(project)
    override fun getBeforeCheckinConfigurationPanel(): RefreshableOnComponent =
        BooleanCommitOption(panel, "Check unsaved Unity scenes", false,
            settings::checkUnsavedScenes)

    override fun beforeCheckin(
        executor: CommitExecutor?,
        additionalDataConsumer: PairConsumer<Any, Any>
    ): ReturnResult {
        if (settings.checkUnsavedScenes)
        {
            var providerResult = false
            try {
                providerResult = project.solution.frontendBackendModel.hasUnsavedScenes
                    .sync(Unit, RpcTimeouts(200L, 200L))
            } catch (t: Throwable) {
                logger.warn("Unable to fetch hasUnsavedScenes", t)
            }

            if (providerResult) return askUser()
        }

        return ReturnResult.COMMIT
    }

    private fun askUser(): ReturnResult {
        val dialogResult = Messages.showOkCancelDialog(
            project,
            "Unsaved changes in Unity scenes will not be included in the commit",
            "Unsaved Unity Scenes",
            "Commit Anyway",
            "Cancel",
            Messages.getWarningIcon()
        )

        if (dialogResult == Messages.OK) return ReturnResult.COMMIT
        return ReturnResult.CANCEL
    }
}