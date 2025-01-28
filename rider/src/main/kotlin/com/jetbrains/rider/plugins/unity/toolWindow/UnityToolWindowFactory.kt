package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.impl.status.StatusBarUtil
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelView
import icons.UnityIcons

//there's an API for registering tool windows in the IJ Platform
@Service(Service.Level.PROJECT)
class UnityToolWindowFactory(private val project: Project) {

    companion object {
        const val TOOL_WINDOW_ID = "Unity"
        const val ACTION_PLACE = "Unity"

        fun getInstance(project: Project): UnityToolWindowFactory = project.getService(UnityToolWindowFactory::class.java)

        fun show(project: Project) {
            ToolWindowManager.getInstance(project).getToolWindow(TOOL_WINDOW_ID)?.show(null)
        }
    }

    private val lock = Object()
    private var context: UnityToolWindowContext? = null

    fun getOrCreateContext(): UnityToolWindowContext {
        synchronized(lock) {
            return context ?: create()
        }
    }

    // TODO: Use ToolWindowFactory/RiderNuGetToolWindowFactory and toolWindow extension points
    @Suppress("DEPRECATION")
    private fun create(): UnityToolWindowContext {
        val toolWindow = ToolWindowManager.getInstance(project).registerToolWindow(TOOL_WINDOW_ID, true, ToolWindowAnchor.BOTTOM, project,
                                                                                   true, false)
        val contentManager = toolWindow.contentManager
        toolWindow.title = ""
        toolWindow.setIcon(UnityIcons.ToolWindows.UnityLog)

        val logModel = UnityLogPanelModel(UnityProjectLifetimeService.getLifetime(project), project, toolWindow)
        val logView = UnityLogPanelView(UnityProjectLifetimeService.getLifetime(project), project, logModel, toolWindow)
        val toolWindowContent = contentManager.factory.createContent(null, UnityBundle.message("tab.title.log"), true).apply {
            StatusBarUtil.setStatusBarInfo(project, "")
            component = logView.panel
            isCloseable = false
        }

        contentManager.addContent(toolWindowContent)
        val twContext = UnityToolWindowContext(toolWindow, logModel)
        context = twContext
        return twContext
    }
}