package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.CommitExecutor
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.openapi.vcs.ui.RefreshableOnComponent
import com.intellij.util.PairConsumer
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

/**
 * Detect Empty Folder .meta when making a commit
 * https://youtrack.jetbrains.com/issue/RIDER-75587
 *
 * 1. When you create an (empty)folder, Unity will create a .meta file for that folder.
 * Git does not commit directory structure, so the empty folder will not enter VCS.
 * We can help by providing action to uncheck those

 * Maybe
 * 2. When you commit file, check that meta is also committed.
*/
class MetaFilesCheckinHandlerFactory : CheckinHandlerFactory() {
    override fun createHandler(panel: CheckinProjectPanel, commitContext: CommitContext): CheckinHandler =
        MetaFilesCheckHandler(panel)
}

private val logger = Logger.getInstance(MetaFilesCheckinHandlerFactory::class.java)

private class MetaFilesCheckHandler(
    private val panel: CheckinProjectPanel
) : CheckinHandler() {

    private val project = panel.project
    private val changes = panel.selectedChanges
    private val settings = MetaFilesCheckinState.getService(project)
    override fun getBeforeCheckinConfigurationPanel(): RefreshableOnComponent? {
        if (!project.isUnityProject())
            return null

        return BooleanCommitOption(
            panel, UnityUIBundle.message("commitOption.check.meta.files"), false,
            settings::checkMetaFiles
        )
    }

    override fun beforeCheckin(
        executor: CommitExecutor?,
        additionalDataConsumer: PairConsumer<Any, Any>
    ): ReturnResult {
        if (settings.checkMetaFiles && project.isUnityProject()) {
            if (changes.any()){
                logger.warn(UnityUIBundle.message("commitOption.check.meta.files"))
                return ReturnResult.CANCEL
            }

        }

        return ReturnResult.COMMIT
    }
}