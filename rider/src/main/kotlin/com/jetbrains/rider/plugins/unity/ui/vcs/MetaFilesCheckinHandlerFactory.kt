package com.jetbrains.rider.plugins.unity.ui.vcs

import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.vcs.CheckinProjectPanel
import com.intellij.openapi.vcs.FileStatus
import com.intellij.openapi.vcs.changes.CommitContext
import com.intellij.openapi.vcs.changes.CommitExecutor
import com.intellij.openapi.vcs.changes.ui.BooleanCommitOption
import com.intellij.openapi.vcs.checkin.CheckinHandler
import com.intellij.openapi.vcs.checkin.CheckinHandlerFactory
import com.intellij.openapi.vcs.ui.RefreshableOnComponent
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.util.PairConsumer
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectDir
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
            panel, UnityUIBundle.message("attempt.to.commit.an.empty.folder.meta.files"), false,
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
                val emptyFolders = changes.filter {
                    val virtualFile = it.virtualFile ?: return@filter false
                    if (!(it.fileStatus == FileStatus.ADDED
                            && isMetaFile(it.virtualFile))) return@filter false
                    val folder = virtualFile.parent.findChild(virtualFile.nameWithoutExtension) ?: return@filter false
                    if (!folder.isDirectory) return@filter false
                    return@filter !folder.children.any()
                }

                if (emptyFolders.any()){
                    logger.info("attempt.to.commit.an.empty.folder.meta.files")
                    val groupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity commit failure")
                    val title = UnityUIBundle.message("attempt.to.commit.an.empty.folder.meta.files")
                    val message = UnityUIBundle.message("notification.content.empty.folders.are.not.under.git.index.prevent.committing.its.metafile",
                        emptyFolders.joinToString(separator = ", ") {
                            project.solutionDirectory.toPath().relativize(it.virtualFile!!.toNioPath()).pathString
                        })
                    val notification = Notification(groupId.displayId, title, message, NotificationType.ERROR)
                    Notifications.Bus.notify(notification, project)

                    return ReturnResult.CLOSE_WINDOW
                }
            }
        }
        return ReturnResult.COMMIT
    }

    private fun isMetaFile(file: VirtualFile?): Boolean {
        val extension = file?.extension ?: return false
        return "meta".equals(extension, true)
    }
}