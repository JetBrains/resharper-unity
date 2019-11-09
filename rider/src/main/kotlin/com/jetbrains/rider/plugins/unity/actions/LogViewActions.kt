package com.jetbrains.rider.plugins.unity.actions

import com.intellij.notification.*
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.OpenFileDescriptor
import com.intellij.openapi.vfs.VfsUtil
import com.jetbrains.rider.model.frontendBackendModel
import icons.UnityIcons
import com.jetbrains.rider.projectView.solution
import java.io.File

class RiderUnityOpenEditorLogAction : RiderUnityLogViewAction("Open Unity Editor Log", "", UnityIcons.Actions.OpenEditorLog) {

    companion object {
        private val logger = Logger.getInstance(RiderUnityOpenEditorLogAction::class.java)
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val f = project.solution.frontendBackendModel.editorLogPath.valueOrNull
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
                val groupId = NotificationGroup("Unity log open", NotificationDisplayType.BALLOON, true)
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

class RiderUnityOpenPlayerLogAction : RiderUnityLogViewAction("Open Unity Player Log", "", UnityIcons.Actions.OpenPlayerLog) {

    companion object {
        private val logger = Logger.getInstance(RiderUnityOpenPlayerLogAction::class.java)
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val f = project.solution.frontendBackendModel.playerLogPath.valueOrNull
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
                val groupId = NotificationGroup("Unity log open", NotificationDisplayType.BALLOON, true)
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