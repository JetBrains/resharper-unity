package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.actions.InstallEditorPluginAction
import com.jetbrains.rider.projectView.solution

class OutOfSyncEditorNotification(project: Project): ProtocolSubscribedProjectComponent(project) {
    companion object {
        private val notificationGroupId = NotificationGroup.balloonGroup("Unity connection is out of sync")
    }

    init {
        project.solution.rdUnityModel.onEditorModelOutOfSync.adviseNotNull(projectComponentLifetime) {
            val message = "The Unity editor plugin is out of date and automatic plugin updates are disabled. Advanced Unity integration features are unavailable until the plugin is updated."

            val notification = Notification(notificationGroupId.displayId, "Unity editor plugin update required", message, NotificationType.WARNING)
            notification.addAction(object : InstallEditorPluginAction(){})
            Notifications.Bus.notify(notification, project)
        }
    }
}