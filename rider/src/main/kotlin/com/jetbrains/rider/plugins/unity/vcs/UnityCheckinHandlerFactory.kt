package com.jetbrains.rider.plugins.unity.vcs

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.CommitExecutor
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.util.PairConsumer
import com.jetbrains.rd.framework.impl.RpcTimeouts
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

/**
 * Checks if there are unsaved scenes in Unity.
 */
class UnsavedSceneCheckinHandlerFactory : CheckinHandlerFactory() {
    override fun createHandler(panel: CheckinProjectPanel, commitContext: CommitContext): CheckinHandler =
        UnresolvedMergeCheckHandler(panel, commitContext)
}

private val logger = Logger.getInstance(UnsavedSceneCheckinHandlerFactory::class.java)

private class UnresolvedMergeCheckHandler(
    private val panel: CheckinProjectPanel,
    private val commitContext: CommitContext
) : CheckinHandler() {

    override fun beforeCheckin(
        executor: CommitExecutor?,
        additionalDataConsumer: PairConsumer<Any, Any>
    ): ReturnResult {
        var providerResult = false
        try {
            providerResult = panel.project.solution.frontendBackendModel.hasUnsavedScenes
                .sync(Unit, RpcTimeouts(200L, 200L))
        } catch (t: Throwable) {
            logger.warn("Error fetching hasUnsavedScenes")
        }

        if (providerResult) return askUser()
        return ReturnResult.COMMIT
    }

    private fun askUser(): ReturnResult {
        val answer = Messages.showYesNoDialog(
            panel.component, "Commit anyway?",
            "Unsaved Scenes in Unity", Messages.getWarningIcon()
        )
        return if (answer != Messages.YES) ReturnResult.CANCEL else ReturnResult.COMMIT
    }
}