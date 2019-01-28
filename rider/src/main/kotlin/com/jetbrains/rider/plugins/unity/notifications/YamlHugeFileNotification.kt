package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rd.util.reactive.fire
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.UnityHost
import javax.swing.event.HyperlinkEvent

class YamlHugeFileNotification(project: Project, private val unityHost: UnityHost): LifetimedProjectComponent(project) {

    companion object {
        private val notificationGroupId = NotificationGroup.balloonGroup("Unity Enable Yaml")
    }

    init {
        unityHost.model.notifyYamlHugeFiles.adviseNotNullOnce(componentLifetime){
            showNotificationIfNeeded()
        }
    }

    private fun showNotificationIfNeeded(){

        val message = """Due to the size of the project, parsing of Unity scenes, assets and prefabs has been disabled. Usages of C# code in these files will not be detected. Re-enabling can impact initial file processing.
            <ul style="margin-left:10px">
              <li><a href="turnOnYamlParsing">Turn on anyway</a></li>
            </ul>
            """

        val yamlNotification = Notification(notificationGroupId.displayId, "Disabled parsing of Unity assets", message, NotificationType.WARNING)
        yamlNotification.setListener { notification, hyperlinkEvent ->
            if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                return@setListener

            if (hyperlinkEvent.description == "turnOnYamlParsing"){
                unityHost.model.enableYamlParsing.fire()
                notification.hideBalloon()
            }
        }

        Notifications.Bus.notify(yamlNotification, project)
    }
}