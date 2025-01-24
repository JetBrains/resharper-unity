package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.impl.status.StatusBarUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.LogEvent
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelView
import com.jetbrains.rider.ui.RiderToolWindowFactory

class UnityToolWindowFactory : RiderToolWindowFactory() {

    companion object {
        const val TOOLWINDOW_ID = "Unity"
        const val ACTION_PLACE = "Unity"

        lateinit var logModel :UnityLogPanelModel

        fun getToolWindow(project: Project) = ToolWindowManager.getInstance(project).getToolWindow(TOOLWINDOW_ID)

        fun show(project: Project) {
            getToolWindow(project)?.show()
        }

        fun activateToolWindowIfNotActive(project: Project) {
            val toolWindow = getToolWindow(project)
            if (toolWindow?.isActive != null && toolWindow.isActive == false) {
                toolWindow.activate {}
            }
        }

        fun addEvent(project: Project, event: LogEvent) {
            getToolWindow(project)
            logModel.events.addEvent(event)
        }
    }

    override fun shouldBeAvailable(project: Project): Boolean {
        return false
    }

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow, lifetime: Lifetime) {
        toolWindow.isAvailable = true
        logModel = UnityLogPanelModel(UnityProjectLifetimeService.getLifetime(project), project, toolWindow)
        val logView = UnityLogPanelView(UnityProjectLifetimeService.getLifetime(project), project, logModel, toolWindow)
        val contentManager = toolWindow.contentManager
        val toolWindowContent = contentManager.factory.createContent(null, UnityBundle.message("tab.title.log"), true).apply {
            StatusBarUtil.setStatusBarInfo(project, "")
            component = logView.panel
            isCloseable = false
        }

        contentManager.addContent(toolWindowContent)
    }
}