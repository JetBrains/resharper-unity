package com.jetbrains.rider.settings

import com.intellij.ide.GeneralSettings
import com.intellij.ide.actions.ShowSettingsUtilImpl
import com.intellij.ide.ui.UISettings
import com.intellij.ide.ui.search.SearchUtil
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.Notification
import com.intellij.notification.NotificationGroup
import com.intellij.notification.NotificationType
import com.intellij.notification.Notifications
import com.intellij.openapi.components.ProjectComponent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.options.Configurable
import com.intellij.openapi.options.ConfigurableGroup
import com.intellij.openapi.options.newEditor.SettingsDialogFactory
import com.intellij.openapi.project.Project
import com.jetbrains.rider.isUnityProject
import javax.swing.event.HyperlinkEvent

class AutoSaveNotification(private val propertiesComponent: PropertiesComponent, private val project: Project)
    : ProjectComponent {

    private var firstRun = true

    companion object {
        private const val settingName = "do_not_show_unity_auto_save_notification"
        private val notificationGroupId = NotificationGroup.balloonGroup("Unity Disable Auto Save")
        private val logger = Logger.getInstance(RiderShowSettingsUtilImpl::class.java)
    }

    override fun projectOpened() {
        if (project.isUnityProject()) {
            showNotificationIfNeeded()
        }
    }

    private fun showNotificationIfNeeded(){
        if (!firstRun) return
        firstRun = false

        if (propertiesComponent.getBoolean(settingName)) return

        val message = "Saving files may trigger recompilation when switching back to Unity Editor, which may wipe live data if the Unity player is running." +
            "<br/>* <a href=\"disableAutoSave\">Disable</a> auto-save in Rider." +
            "<br/>* <a href=\"doNotShow\">Do not show</a> this notification for this solution."

        val generalSettings = GeneralSettings.getInstance()
        if (generalSettings.isAutoSaveIfInactive || generalSettings.isSaveOnFrameDeactivation){
            val autoSaveNotification = Notification(notificationGroupId.displayId, "Unity: auto-save is enabled", message, NotificationType.WARNING)
            autoSaveNotification.setListener { notification, hyperlinkEvent ->
                if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                    return@setListener

                if (hyperlinkEvent.description == "disableAutoSave"){
                    disableAutoSaveAndNotify()
                }

                if (hyperlinkEvent.description == "doNotShow"){
                    propertiesComponent.setValue(settingName, true)
                    notification.hideBalloon()
                }
            }

            Notifications.Bus.notify(autoSaveNotification, project)
        }
    }

    private fun disableAutoSaveAndNotify(){
        val generalSettings = GeneralSettings.getInstance()

        generalSettings.isSaveOnFrameDeactivation = false
        generalSettings.isAutoSaveIfInactive = false
        UISettings.instance.markModifiedTabsWithAsterisk = true

        val message = "The following settings were changed:" +
            "<br/>* <a href=\"openSettings_System Settings_frame deactivation\">Save files on frame deactivation</a>" +
            "<br/>* <a href=\"openSettings_System Settings_Save files automatically\">Save files automatically if application is idle</a>" +
            "<br/>* <a href=\"openSettings_Editor Tabs_Mark modified tabs\">Mark modified tabs with asterisk</a>"

        val notification = Notification(notificationGroupId.displayId, "Unity: auto-save was disabled", message, NotificationType.INFORMATION)
        notification.setListener { _, hyperlinkEvent ->
            if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED)
                return@setListener

            if (hyperlinkEvent.description.startsWith("openSettings_")) {
                val setting = hyperlinkEvent.description.replace("openSettings_", "")
                showSettings(setting)
            }
        }

        Notifications.Bus.notify(notification, project)
    }

    private fun showSettings(setting: String){
        try {
            val split = setting.indexOf('_')
            if (split == setting.length - 1) return

            val groupName = if (split == -1) setting else setting.substring(0, split)
            val filter = if (split == -1) "" else setting.substring(split + 1, setting.length)
            SettingsViewModelHost.getOrCreate(project)
            showDialog(groupName, filter)
        } catch (e: Exception) {
            logger.error(e)
        }
    }

    private fun showDialog(groupName: String, filter: String){
        var groups = ShowSettingsUtilImpl.getConfigurableGroups(project, true)
        groups = groups.filter { it.configurables.isNotEmpty() }.toTypedArray()

        val configurable2Select = findPreselectedByDisplayName(groupName, groups)
        SettingsDialogFactory.getInstance().create(project, groups, configurable2Select, filter).show()
    }

    private fun findPreselectedByDisplayName(preselectedConfigurableDisplayName: String, groups: Array<ConfigurableGroup>): Configurable? {
        return SearchUtil.expand(groups).firstOrNull { preselectedConfigurableDisplayName == it.displayName }
    }
}