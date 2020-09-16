package com.jetbrains.rider.plugins.unity.actions

import com.intellij.notification.*
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import java.io.File

class RiderUnityOpenEditorLogAction : RiderUnityLogViewAction() {

    companion object {
        const val actionId = "RiderUnityOpenEditorLogAction"
        private val logger = Logger.getInstance(RiderUnityOpenEditorLogAction::class.java)
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val f = project.solution.rdUnityModel.editorLogPath.valueOrNull
        if (f!=null)
        {
            val vf = VfsUtil.findFileByIoFile(File(f), true)
            if (vf!=null)
            {
                val descriptor = OpenFileDescriptor(project, vf)
                FileEditorManager.getInstance(project).openTextEditor(descriptor, true)
            }
            else
            {
                val groupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity log open");
                val title = "Could not open Unity Editor Log"
                val message = "$f is not present."
                val notification = Notification(groupId.displayId, title, message, NotificationType.INFORMATION)
                Notifications.Bus.notify(notification, project)
            }
        }
        else
        {
            logger.error("Could not open Unity Editor Log, path was null")
        }
    }
}

class RiderUnityOpenPlayerLogAction : RiderUnityLogViewAction() {

    companion object {
        const val actionId = "RiderUnityOpenPlayerLogAction"
        private val logger = Logger.getInstance(RiderUnityOpenPlayerLogAction::class.java)
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val f = project.solution.rdUnityModel.playerLogPath.valueOrNull
        if (f!=null)
        {
            val vf = VfsUtil.findFileByIoFile(File(f), true)
            if (vf!=null)
            {
                val descriptor = OpenFileDescriptor(project, vf)
                FileEditorManager.getInstance(project).openTextEditor(descriptor, true)
            }
            else
            {
                val groupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity log open");
                val title = "Could not open Unity Player Log"
                val message = "$f is not present."
                val notification = Notification(groupId.displayId, title, message, NotificationType.INFORMATION)
                Notifications.Bus.notify(notification, project)
            }
        }
        else
        {
            logger.error("Could not open Unity Player Log, path was null")
        }
    }
}