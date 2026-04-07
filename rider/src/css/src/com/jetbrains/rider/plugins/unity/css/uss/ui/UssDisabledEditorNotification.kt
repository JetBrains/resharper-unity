package com.jetbrains.rider.plugins.unity.css.uss.ui

import com.intellij.ide.plugins.PluginManagerCore
import com.intellij.ide.plugins.PluginManagerMain
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.extensions.PluginId
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotificationProvider
import com.intellij.ui.EditorNotifications
import com.jetbrains.rider.plugins.unity.css.uss.impl.UnityCssBundle
import com.jetbrains.rider.plugins.unity.isUnityProject
import java.util.function.Function
import javax.swing.JComponent

internal class UssDisabledEditorNotification : EditorNotificationProvider {

    companion object {
        private const val DO_NOT_SHOW_AGAIN_KEY = "unity.uss.css.plugin.disabled.do.not.show"
        private const val CSS_PLUGIN_ID = "com.intellij.css"
    }

    override fun collectNotificationData(project: Project, file: VirtualFile): Function<in FileEditor, out JComponent?>? {
        if (project.isUnityProject.value && isUssFileSafe(file) && PluginManagerCore.isDisabled(PluginId.getId(CSS_PLUGIN_ID))) {
            if (PropertiesComponent.getInstance(project).getBoolean(DO_NOT_SHOW_AGAIN_KEY, false)) {
                return null
            }

            return Function {
                EditorNotificationPanel().also { panel ->
                    panel.text(UnityCssBundle.message("uss.disabled.editor.notification.panel.text"))
                    panel.createActionLabel(UnityCssBundle.message("uss.disabled.editor.notification.enable.css.plugin")) {
                        // TODO: Maybe in 2020.2 we can do this dynamically without restart?
                        // That would require enabling the CSS plugin dynamically, and then enabling our PluginCssPart.xml part
                        // dynamically, too
                        PluginManagerCore.enablePlugin(PluginId.getId(CSS_PLUGIN_ID))
                        PluginManagerMain.notifyPluginsUpdated(project)
                        EditorNotifications.getInstance(project).updateAllNotifications()
                    }
                    panel.createActionLabel(UnityCssBundle.message("don.t.show.again")) {
                        // Project level - do not show again for this project
                        PropertiesComponent.getInstance(project).setValue(DO_NOT_SHOW_AGAIN_KEY, true)
                        EditorNotifications.getInstance(project).updateAllNotifications()
                    }
                }
            }
        }

        return null
    }

    private fun isUssFileSafe(file: VirtualFile): Boolean {
        // We can't check for UssFileType because it won't be loaded. The file type is part of the USS language, which
        // derives from CSSLanguage, which will be unavailable.
        return file.extension.equals("uss", true)
    }
}