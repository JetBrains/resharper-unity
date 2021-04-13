package com.jetbrains.rider.plugins.unity.vcs

import com.intellij.openapi.ui.Messages
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vcs.VcsBundle.message
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.CommitExecutor
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.openapi.vcs.checkin.UnresolvedMergeCheckProvider
import com.intellij.util.PairConsumer

/**
 * Checks if there are unsaved scenes in Unity.
 */
class UnsavedSceneCheckinHandlerFactory : CheckinHandlerFactory() {
    override fun createHandler(panel: CheckinProjectPanel, commitContext: CommitContext): CheckinHandler =
        UnresolvedMergeCheckHandler(panel, commitContext)
}

private val MERGE_STATUSES = setOf(
    FileStatus.MERGE,
    FileStatus.MERGED_WITH_BOTH_CONFLICTS,
    FileStatus.MERGED_WITH_CONFLICTS,
    FileStatus.MERGED_WITH_PROPERTY_CONFLICTS
)

private class UnresolvedMergeCheckHandler(
    private val panel: CheckinProjectPanel,
    private val commitContext: CommitContext
) : CheckinHandler() {

    override fun beforeCheckin(
        executor: CommitExecutor?,
        additionalDataConsumer: PairConsumer<Any, Any>
    ): ReturnResult {
        val providerResult = UnresolvedMergeCheckProvider.EP_NAME.extensions.asSequence()
            .mapNotNull { it.checkUnresolvedConflicts(panel, commitContext, executor) }
            .firstOrNull()
        return providerResult ?: performDefaultCheck()
    }

    private fun performDefaultCheck(): ReturnResult =
        if (panel.hasUnresolvedConflicts()) askUser() else ReturnResult.COMMIT

    private fun askUser(): ReturnResult {
        val answer = Messages.showYesNoDialog(
            panel.component, message(
                "checkin.unresolved.merge.are.you.sure.you.want.to.commit.changes.with.unresolved.conflicts"
            ),
            message("checkin.unresolved.merge.unresolved.conflicts"), Messages.getWarningIcon()
        )
        return if (answer != Messages.YES) ReturnResult.CANCEL else ReturnResult.COMMIT
    }

    private fun CheckinProjectPanel.hasUnresolvedConflicts() =
        selectedChanges.any { it.fileStatus in MERGE_STATUSES }
}