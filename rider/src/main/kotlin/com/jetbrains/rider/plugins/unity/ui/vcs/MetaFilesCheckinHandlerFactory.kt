package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vcs.changes.Change
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.CommitExecutor
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.openapi.vcs.ui.RefreshableOnComponent
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.PairConsumer
import com.intellij.vcsUtil.VcsUtil
import com.jetbrains.rider.plugins.unity.explorer.MetaTracker
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
 * 3. When file is removed - check that its meta file is removed.
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
            val changes = panel.selectedChanges
            if (changes.any()) {
                logger.info(UnityUIBundle.message("commitOption.check.meta.files"))
                // attempt to add metafile without main file
                val addedMetaFilesWithoutMainFile = changes.filter {
                    it.virtualFile != null
                    it.fileStatus == FileStatus.ADDED
                        && isMetaFile(it.virtualFile)
                        && !changes.any {change->
                            change.fileStatus == FileStatus.ADDED &&
                            MetaTracker.getMetaFile(change.virtualFile?.path) == it.virtualFile?.toNioPath()
                        }
                }
                val addedMetaWOMainFile = addedMetaFilesWithoutMainFile.filter {
                        val virtualFile = it.virtualFile ?: return@filter false
                        val file = virtualFile.path.substring(0, virtualFile.path.length - 5) // trim ".meta"
                        return@filter VcsUtil.isFileUnderVcs(project, file)
                }
                if (addedMetaWOMainFile.any()) return askUser()
            }

        }

        return ReturnResult.COMMIT
    }

    private fun askUser(): ReturnResult {
        val dialogResult = Messages.showOkCancelDialog(
            project,
            "Proceed?",
            "Attempt to commit meta file without corresponding asset",
            UnityUIBundle.message("dialog.unsaved.button.commit.anyway"),
            UnityUIBundle.message("dialog.unsaved.button.cancel"),
            Messages.getWarningIcon()
        )

        if (dialogResult == Messages.OK) return ReturnResult.COMMIT
        return ReturnResult.CANCEL
    }

    private fun isMetaFile(file: VirtualFile?): Boolean {
        val extension = file?.extension ?: return false
        return "meta".equals(extension, true)
    }
}