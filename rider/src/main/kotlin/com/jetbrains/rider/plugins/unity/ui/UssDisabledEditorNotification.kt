package com.jetbrains.rider.plugins.unity.ui

import com.intellij.ide.plugins.PluginManager
import com.intellij.ide.plugins.PluginManagerMain
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.fileTypes.FileTypeRegistry
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotifications
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss.UssFileType
import com.jetbrains.rider.plugins.unity.util.isUssFile

class UssDisabledEditorNotification: EditorNotifications.Provider<EditorNotificationPanel>() {

    companion object {
        private val KEY = Key.create<EditorNotificationPanel>("unity.uss.css.plugin.disabled.notification.panel")
        private const val DO_NOT_SHOW_AGAIN_KEY = "unity.uss.css.plugin.disabled.do.not.show"
        private const val CSS_PLUGIN_ID = "com.intellij.css"
    }

    override fun getKey(): Key<EditorNotificationPanel> = KEY

    override fun createNotificationPanel(file: VirtualFile, fileEditor: FileEditor, project: Project): EditorNotificationPanel? {
        if (project.isUnityProject() && isUssFile(file) && PluginManager.isDisabled(CSS_PLUGIN_ID)) {
            if (PropertiesComponent.getInstance(project).getBoolean(DO_NOT_SHOW_AGAIN_KEY, false)) {
                return null
            }

            val panel = EditorNotificationPanel()
            panel.text("USS support requires the CSS plugin to be enabled")
            panel.createActionLabel("Enable CSS plugin") {
                // TODO: Maybe in 2020.1 we can do this dynamically without restart?
                // That would require enabling the CSS plugin dynamically, and then enabling our PluginCssPart.xml part
                // dynamically, too
                PluginManager.enablePlugin(CSS_PLUGIN_ID)
                PluginManagerMain.notifyPluginsUpdated(project)
                EditorNotifications.getInstance(project).updateAllNotifications()
            }
            panel.createActionLabel("Don't show again") {
                // Project level - do not show again for this project
                PropertiesComponent.getInstance(project).setValue(DO_NOT_SHOW_AGAIN_KEY, true)
                EditorNotifications.getInstance(project).updateAllNotifications()
            }
            return panel
        }

        return null
    }
}