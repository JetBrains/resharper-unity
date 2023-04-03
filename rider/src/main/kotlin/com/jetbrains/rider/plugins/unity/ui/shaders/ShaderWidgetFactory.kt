package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidgetFactory
import com.intellij.openapi.wm.impl.status.widget.StatusBarWidgetsManager
import com.intellij.util.application
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solutionDirectory

class ShaderWidgetFactory: StatusBarWidgetFactory {
    override fun getId() = "ShaderWidget"
    override fun canBeEnabledOn(statusBar: StatusBar) = true
    override fun getDisplayName() = UnityUIBundle.message("unity.shader.file.context")
    override fun disposeWidget(widget: StatusBarWidget) {}
    override fun createWidget(project: Project) = ShaderWidget(project)

    override fun isAvailable(project: Project): Boolean {

        // We should not call VFS refresh here, it is project frame initialization
        // This code can be simplified when this issue will be fixed in IJ platform:
        // https://youtrack.jetbrains.com/issue/IJPL-73/Off-load-initialization-of-StatusBarWidgetsManager-to-BGT

        val solutionDirectory = project.solutionDirectory.toVirtualFile(false)
        if (solutionDirectory != null) {
            return UnityProjectDiscoverer.getInstance(project).isUnityProject
        }

        application.executeOnPooledThread {
            project.solutionDirectory.toVirtualFile(true)
            application.invokeLater {
                project.service<StatusBarWidgetsManager>().updateWidget(ShaderWidgetFactory::class.java)
            }
        }
        return false
    }
}