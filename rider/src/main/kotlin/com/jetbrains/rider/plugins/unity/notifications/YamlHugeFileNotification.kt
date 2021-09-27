package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.notification.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rd.util.reactive.fire
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import javax.swing.event.HyperlinkEvent

class YamlHugeFileNotification(project: Project): ProtocolSubscribedProjectComponent(project) {

    companion object {
        private val notificationGroupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity Enable Yaml")
    }

    init {
        project.solution.frontendBackendModel.notifyYamlHugeFiles.adviseNotNullOnce(projectComponentLifetime){
            showNotificationIfNeeded()
        }
    }

    private fun showNotificationIfNeeded(){

        val message = """Due to the size of the project, indexing of Unity scenes, assets and prefabs has been disabled. Usages of C# code in these files will not be detected. Re-enabling can impact initial file processing.
            <ul style="margin-left:10px">
              <li><a href="turnOnYamlParsing">Turn on anyway</a></li>
            </ul>
            """

        val yamlNotification = Notification(notificationGroupId.displayId, "Disabled indexing of Unity assets", message, NotificationType.WARNING)
        yamlNotification.setListener { notification, hyperlinkEvent ->
            if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                return@setListener

            if (hyperlinkEvent.description == "turnOnYamlParsing"){
                project.solution.frontendBackendModel.enableYamlParsing.fire()
                notification.hideBalloon()
            }
        }

        Notifications.Bus.notify(yamlNotification, project)
    }
}
