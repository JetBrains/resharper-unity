package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.BrowserUtil
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.*
import com.intellij.openapi.project.Project
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import javax.swing.event.HyperlinkEvent

class AssetModeForceTextNotification(project: Project): ProtocolSubscribedProjectComponent(project) {

    companion object {
        private const val settingName = "do_not_show_unity_asset_mode_notification"
        private val notificationGroupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity Asset Mode")
    }

    init {
        project.solution.frontendBackendModel.notifyAssetModeForceText.adviseNotNullOnce(projectComponentLifetime){
            showNotificationIfNeeded()
        }
    }

    private fun showNotificationIfNeeded() {

        if (PropertiesComponent.getInstance(project).getBoolean(settingName)) return

        val message = UnityBundle.message("AssetModeForceTextNotification.notification.message.some.advanced.integration.unavailable")
        val assetModeNotification = Notification(notificationGroupId.displayId,
                                                 UnityBundle.message("notification.title.recommend.switching.to.text.asset.serialisation.mode"), message, NotificationType.WARNING)
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