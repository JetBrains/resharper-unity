package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.GeneralSettings
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.util.reactive.adviseNotNullOnce
import javax.swing.event.HyperlinkEvent

class AutoSaveNotification(private val propertiesComponent: PropertiesComponent, project: Project, unityHost: UnityHost): LifetimedProjectComponent(project) {

    private var firstRun = true

    companion object {
        private const val settingName = "do_not_show_unity_auto_save_notification"
        private val notificationGroupId = NotificationGroup.balloonGroup("Unity Disable Auto Save")
    }

    init {
        unityHost.model.notifyIsRecompileAndContinuePlaying.adviseNotNullOnce(componentLifetime){
            showNotificationIfNeeded(it)
        }
    }

    private fun showNotificationIfNeeded(tabName : String){
        if (!firstRun) return
        firstRun = false

        if (propertiesComponent.getBoolean(settingName)) return

        val message = "Auto save is enabled in Rider. This can cause unwanted recompilation and the loss of current state during play mode." +
            "<br/>* Consider changing the <i>Script Changes While Playing</i> in Unity Preferences $tabName tab." +
            "<br/>* <a href=\"doNotShow\">Do not show</a> this notification for this solution."

        val generalSettings = GeneralSettings.getInstance()
        if (generalSettings.isAutoSaveIfInactive || generalSettings.isSaveOnFrameDeactivation){
            val autoSaveNotification = Notification(notificationGroupId.displayId, "Unity: scripts reload while Playing", message, NotificationType.WARNING)
            autoSaveNotification.setListener { notification, hyperlinkEvent ->
                if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                    return@setListener

                if (hyperlinkEvent.description == "doNotShow"){
                    propertiesComponent.setValue(settingName, true)
                    notification.hideBalloon()
                }
            }

            Notifications.Bus.notify(autoSaveNotification, project)
        }
    }
}