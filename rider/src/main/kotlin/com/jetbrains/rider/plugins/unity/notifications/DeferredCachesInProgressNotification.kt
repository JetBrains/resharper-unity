package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.MessageType
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.ex.StatusBarEx
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.UnityHost


class DeferredCachesInProgressNotification(project: Project, unityHost: UnityHost): LifetimedProjectComponent(project) {

    init {
        unityHost.model.showDeferredCachesProgressNotification.adviseNotNull(componentLifetime) {
            UIUtil.invokeLaterIfNeeded {
                val ideFrame = WindowManager.getInstance().getIdeFrame(project)
                if (ideFrame != null) {
                    (ideFrame.statusBar as StatusBarEx?)!!.notifyProgressByBalloon(MessageType.WARNING,
                        "Usages in assets are not available during initial asset indexing")
                }
            }
        }
    }
}