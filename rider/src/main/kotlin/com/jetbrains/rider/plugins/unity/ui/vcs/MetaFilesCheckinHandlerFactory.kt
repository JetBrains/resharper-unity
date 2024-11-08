package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.ide.BrowserUtil
import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption.Companion.create
import com.intellij.openapi.vcs.checkin.*
import com.intellij.openapi.vcs.ui.RefreshableOnComponent
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solutionDirectory
import kotlin.io.path.pathString

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
        MetaFilesCommitCheck(panel.project)
}

private class MetaFilesCommitCheck(private val project: Project) : CheckinHandler(), CommitCheck, DumbAware {
    override fun getExecutionOrder(): CommitCheck.ExecutionOrder = CommitCheck.ExecutionOrder.LATE
    private val settings = MetaFilesCheckinState.getService(project)
    override fun isEnabled(): Boolean {
        return settings.checkMetaFiles && project.isUnityProject.value
    }

    override fun getBeforeCheckinConfigurationPanel(): RefreshableOnComponent? {
        if (!project.isUnityProject.value)
            return null
        return create(project, this, false, UnityUIBundle.message("check.redundant.meta.files"),
                      settings::checkMetaFiles)
    }

    override suspend fun runCheck(commitInfo: CommitInfo): CommitProblem? {
        if (settings.checkMetaFiles && project.isUnityProject.value) {
            val emptyFolders = commitInfo.committedChanges.filter {
                val virtualFile = it.virtualFile ?: return@filter false
                if (!(it.fileStatus == FileStatus.ADDED
                      && isMetaFile(it.virtualFile))) return@filter false
                val folder = virtualFile.parent.findChild(virtualFile.nameWithoutExtension) ?: return@filter false
                if (!folder.isDirectory) return@filter false
                return@filter !folder.children.any()
            }
            if (!emptyFolders.any()) return null
            val message =
                UnityUIBundle.message("notification.content.empty.folders.so.meta.files.should.not.be.committed",
                                      emptyFolders.take(3).joinToString(separator = ", ") {
                                          project.solutionDirectory.toPath().relativize(it.virtualFile!!.toNioPath()).pathString
                                      } + if (emptyFolders.count() > 3) ", …"
                                      else {
                                          ""
                                      })
            return EmptyFoldersCommitProblem(message)
        }
        return null
    }

    private fun isMetaFile(file: VirtualFile?): Boolean {
        val extension = file?.extension ?: return false
        return "meta".equals(extension, true)
    }

    class EmptyFoldersCommitProblem(message: String) : CommitProblemWithDetails {
        override val showDetailsAction: String = UnityUIBundle.message("unity.before.check.in.show.details")

        override val text: String = message

        override fun showDetails(project: Project) {
            BrowserUtil.browse(EMPTY_FOLDERS_HELP_LINK)
        }
    }

    companion object{
        private const val EMPTY_FOLDERS_HELP_LINK = "https://youtrack.jetbrains.com/issue/RIDER-75587"
    }
}