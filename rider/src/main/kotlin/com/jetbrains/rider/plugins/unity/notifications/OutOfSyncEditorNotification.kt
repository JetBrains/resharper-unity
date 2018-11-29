package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.actions.InstallEditorPluginAction
import com.jetbrains.rider.util.reactive.adviseNotNull

class OutOfSyncEditorNotification(project: Project, unityHost: UnityHost): LifetimedProjectComponent(project) {
    companion object {
        private val notificationGroupId = NotificationGroup.balloonGroup("Unity connection is out of sync")
    }

    init {
        unityHost.model.onEditorModelOutOfSync.adviseNotNull(componentLifetime) {
            val message = "UnityEditor connection is out of sync and updating EditorPlugin is disabled."

            val notification = Notification(notificationGroupId.displayId, "EditorPlugin update required", message, NotificationType.WARNING)
            notification.addAction(object : InstallEditorPluginAction(){})
            Notifications.Bus.notify(notification, project)
        }
    }
}