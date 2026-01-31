package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.BrowserUtil
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.Notification
import com.intellij.notification.NotificationAction
import com.intellij.notification.NotificationGroupManager
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel

@Service(Service.Level.PROJECT)
class AssetModeForceTextNotification(private val project: Project) {

    companion object {
        private const val settingName = "do_not_show_unity_asset_mode_notification"
        private val notificationGroupId = NotificationGroupManager.getInstance().getNotificationGroup("Unity Asset Mode")
        fun getInstance(project: Project): AssetModeForceTextNotification = project.service()
    }

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.notifyAssetModeForceText.adviseNotNullOnce(lifetime) {
                getInstance(session.project).showNotificationIfNeeded()
            }
        }
    }

    private fun showNotificationIfNeeded() {

        if (PropertiesComponent.getInstance(project).getBoolean(settingName)) return

        val message = UnityBundle.message("AssetModeForceTextNotification.notification.message.some.advanced.integration.unavailable")
        val assetModeNotification = Notification(notificationGroupId.displayId,
                                                 UnityBundle.message("notification.title.recommend.switching.to.text.asset.serialisation.mode"), message, NotificationType.WARNING)

        assetModeNotification.addAction(object : NotificationAction(
            UnityBundle.message("read.more")) {
            override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                val url = "https://github.com/JetBrains/resharper-unity/wiki/Asset-serialization-mode"
                BrowserUtil.browse(url)
            }
        })
        assetModeNotification.addAction(object : NotificationAction(
            UnityBundle.message("link.label.do.not.show.again")) {
            override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                PropertiesComponent.getInstance(project).setValue(settingName, true)
            }
        })
        Notifications.Bus.notify(assetModeNotification, project)
    }
}