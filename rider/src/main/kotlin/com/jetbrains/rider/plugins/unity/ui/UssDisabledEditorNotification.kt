package com.jetbrains.rider.plugins.unity.ui

import com.intellij.ide.plugins.*
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.extensions.PluginId
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotifications
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.util.isUssFile

class UssDisabledEditorNotification: EditorNotifications.Provider<EditorNotificationPanel>() {

    companion object {
        private val KEY = Key.create<EditorNotificationPanel>("unity.uss.css.plugin.disabled.notification.panel")
        private const val DO_NOT_SHOW_AGAIN_KEY = "unity.uss.css.plugin.disabled.do.not.show"
        private const val CSS_PLUGIN_ID = "com.intellij.css"
    }

    override fun getKey(): Key<EditorNotificationPanel> = KEY

    override fun createNotificationPanel(file: VirtualFile, fileEditor: FileEditor, project: Project): EditorNotificationPanel? {
        if (project.isUnityProject() && isUssFileSafe(file) && PluginManagerCore.isDisabled(PluginId.getId(CSS_PLUGIN_ID))) {
            if (PropertiesComponent.getInstance(project).getBoolean(DO_NOT_SHOW_AGAIN_KEY, false)) {
                return null
            }

            val panel = EditorNotificationPanel()
            panel.text("USS support requires the CSS plugin to be enabled")
            panel.createActionLabel("Enable CSS plugin") {
                // TODO: Maybe in 2020.2 we can do this dynamically without restart?
                // That would require enabling the CSS plugin dynamically, and then enabling our PluginCssPart.xml part
                // dynamically, too
                PluginManagerCore.enablePlugin(PluginId.getId(CSS_PLUGIN_ID))
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

    private fun isUssFileSafe(file: VirtualFile): Boolean {
        // We can't check for UssFileType because it won't be loaded. The file type is part of the USS language, which
        // derives from CSSLanguage, which will be unavailable.
        return file.extension.equals("uss", true)
    }
}