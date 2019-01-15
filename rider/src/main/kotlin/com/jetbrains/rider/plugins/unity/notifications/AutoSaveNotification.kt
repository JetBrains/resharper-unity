package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.BrowserUtil
import com.intellij.ide.GeneralSettings
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.adviseNotNullOnce
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.model.ScriptCompilationDuringPlay
import com.jetbrains.rider.plugins.unity.UnityHost
import javax.swing.event.HyperlinkEvent

class AutoSaveNotification(private val propertiesComponent: PropertiesComponent, project: Project, private val unityHost: UnityHost): LifetimedProjectComponent(project) {

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

        val message = """Unity is configured to compile scripts while in play mode (see $tabName tab in Unity’s preferences). Rider's auto save may cause loss of state in the running game.
            Change Unity to:
            <ul style="margin-left:10px">
              <li><a href="recompileAfterFinishedPlaying">Recompile After Finished Playing</a></li>
              <li><a href="stopPlayingAndRecompile">Stop Playing And Recompile</a></li>
            </ul>
            <a href="doNotShow">Do not show</a> this notification for this solution. <a href="learnMore">Learn more</a>
            """

        val generalSettings = GeneralSettings.getInstance()
        if (generalSettings.isAutoSaveIfInactive || generalSettings.isSaveOnFrameDeactivation){
            val autoSaveNotification = Notification(notificationGroupId.displayId, "Unity configuration issue", message, NotificationType.WARNING)
            autoSaveNotification.setListener { notification, hyperlinkEvent ->
                if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                    return@setListener

                if (hyperlinkEvent.description == "recompileAfterFinishedPlaying"){
                    unityHost.model.setScriptCompilationDuringPlay.fire(ScriptCompilationDuringPlay.RecompileAfterFinishedPlaying)
                    notification.hideBalloon()
                }

                if (hyperlinkEvent.description == "stopPlayingAndRecompile"){
                    unityHost.model.setScriptCompilationDuringPlay.fire(ScriptCompilationDuringPlay.StopPlayingAndRecompile)
                    notification.hideBalloon()
                }

                if (hyperlinkEvent.description == "doNotShow"){
                    propertiesComponent.setValue(settingName, true)
                    notification.hideBalloon()
                }

                if (hyperlinkEvent.description == "learnMore"){
                    BrowserUtil.browse("https://github.com/JetBrains/resharper-unity/issues/707")
                }
            }

            Notifications.Bus.notify(autoSaveNotification, project)
        }
    }
}