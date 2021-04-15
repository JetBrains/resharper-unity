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
                logger.warn("Unable to fetch hasUnsavedScenes")
            }

            if (providerResult) return askUser()
        }

        return ReturnResult.COMMIT
    }

    private fun askUser(): ReturnResult {
        val actionNum = Messages.showDialog(
            project, "Unsaved changes would be missed in commit", "Unsaved Unity Scenes",
            arrayOf("Continue without Saving", "Cancel", "Save Scenes and Commit"), 2, Messages.getWarningIcon()
        )
        if (actionNum == 0) return ReturnResult.COMMIT
        else if (actionNum == 1) return ReturnResult.CANCEL
        try {
            if (project.solution.frontendBackendModel.saveScenes
                .sync(Unit, RpcTimeouts(200L, 200L)))
                return ReturnResult.COMMIT
        } catch (t: Throwable) {
            logger.error("Unable to saveScenes", t)
        }

        return ReturnResult.CANCEL
    }
}