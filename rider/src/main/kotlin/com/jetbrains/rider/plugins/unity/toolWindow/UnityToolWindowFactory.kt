package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.ide.impl.ContentManagerWatcher
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.impl.status.StatusBarUtil
import com.intellij.ui.content.ContentManagerAdapter
import com.intellij.ui.content.ContentManagerEvent
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelView
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.LifetimedProjectComponent

class UnityToolWindowFactory(project: Project,
                             private val toolWindowManager: ToolWindowManager,
                             private val rdUnityHost: UnityHost)
    : LifetimedProjectComponent(project) {

    companion object {
        val TOOLWINDOW_ID = "Unity"
        val ACTION_PLACE = "Unity"
    }

    private val lock = Object()
    private var context: UnityToolWindowContext? = null

    fun getOrCreateContext(): UnityToolWindowContext {
        synchronized(lock) {
            return context ?: create()
        }
    }

    private fun create(): UnityToolWindowContext {
        val toolWindow = toolWindowManager.registerToolWindow(TOOLWINDOW_ID, true, ToolWindowAnchor.BOTTOM, project, true, false)
        val contentManager = toolWindow.contentManager
        contentManager.addContentManagerListener(object : ContentManagerAdapter() {
            override fun contentRemoved(event: ContentManagerEvent?) {
                context = null
                toolWindowManager.unregisterToolWindow(TOOLWINDOW_ID)
            }
        })
        toolWindow.title = ""
        toolWindow.icon = UnityIcons.Logo
        // Required for hiding window without content
        ContentManagerWatcher(toolWindow, contentManager)

        val logModel = UnityLogPanelModel(componentLifetime, project)
        val logView = UnityLogPanelView(project, logModel, rdUnityHost)
        val toolWindowContent = contentManager.factory.createContent(null, "Log", true).apply {
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