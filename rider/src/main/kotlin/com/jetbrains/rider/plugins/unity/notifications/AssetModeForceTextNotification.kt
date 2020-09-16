package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.BrowserUtil
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import javax.swing.event.HyperlinkEvent

class AssetModeForceTextNotification(project: Project): ProtocolSubscribedProjectComponent(project) {

    companion object {
        private const val settingName = "do_not_show_unity_asset_mode_notification"
        private val notificationGroupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity Asset Mode");
    }

    init {
        project.solution.rdUnityModel.notifyAssetModeForceText.adviseNotNullOnce(projectComponentLifetime){
            showNotificationIfNeeded()
        }
    }

    private fun showNotificationIfNeeded() {

        if (PropertiesComponent.getInstance(project).getBoolean(settingName)) return

        val message = """Some advanced integration features are unavailable when the Unity asset serialisation mode is not set to “Force Text”. Enable text serialisation to allow Rider to learn more about the structure of your scenes and assets.
            <ul style="margin-left:10px">
              <li><a href="LearnMoreNavigateAction">Learn more</a></li>
              <li><a href="doNotShow">Do not show</a> this notification for this solution.</li>
            </ul>
            """
        val assetModeNotification = Notification(notificationGroupId.displayId, "Recommend switching to text asset serialisation mode", message, NotificationType.WARNING)
        assetModeNotification.setListener { notification, hyperlinkEvent ->
            if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                return@setListener

            if (hyperlinkEvent.description == "LearnMoreNavigateAction"){
                BrowserUtil.browse("https://github.com/JetBrains/resharper-unity/wiki/Asset-serialization-mode")
                notification.hideBalloon()
            }

            if (hyperlinkEvent.description == "doNotShow"){
                PropertiesComponent.getInstance(project).setValue(settingName, true)
                notification.hideBalloon()
            }
        }


        Notifications.Bus.notify(assetModeNotification, project)
    }
}