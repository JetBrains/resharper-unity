package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.ide.impl.ContentManagerWatcher
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.ex.ToolWindowEx
import com.intellij.openapi.wm.impl.status.StatusBarUtil
import com.intellij.ui.content.ContentManagerAdapter
import com.intellij.ui.content.ContentManagerEvent
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.actions.RiderUnityOpenEditorLogAction
import com.jetbrains.rider.plugins.unity.actions.RiderUnityOpenPlayerLogAction
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelView
import icons.UnityIcons

class UnityToolWindowFactory(project: Project,
                             private val toolWindowManager: ToolWindowManager)
    : LifetimedProjectComponent(project) {

    companion object {
        const val TOOL_WINDOW_ID = "Unity"
        const val ACTION_PLACE = "Unity"

        fun show(project: Project) {
            ToolWindowManager.getInstance(project)?.getToolWindow(TOOL_WINDOW_ID)?.show(null)
        }
    }

    private val lock = Object()
    private var context: UnityToolWindowContext? = null

    fun getOrCreateContext(): UnityToolWindowContext {
        synchronized(lock) {
            return context ?: create()
        }
    }

    private fun create(): UnityToolWindowContext {
        val toolWindow = toolWindowManager.registerToolWindow(TOOL_WINDOW_ID, true, ToolWindowAnchor.BOTTOM, project, true, false)

        if (toolWindow is ToolWindowEx) {
            toolWindow.setAdditionalGearActions(DefaultActionGroup().apply {
                add(RiderUnityOpenEditorLogAction())
                add(RiderUnityOpenPlayerLogAction())
            })
        }

        val contentManager = toolWindow.contentManager
        contentManager.addContentManagerListener(object : ContentManagerAdapter() {
            override fun contentRemoved(event: ContentManagerEvent) {
                context = null
                toolWindowManager.unregisterToolWindow(TOOL_WINDOW_ID)
            }
        })
        toolWindow.title = ""
        toolWindow.icon = UnityIcons.ToolWindows.UnityLog
        // Required for hiding window without content
        ContentManagerWatcher(toolWindow, contentManager)

        val logModel = UnityLogPanelModel(componentLifetime, project)
        val logView = UnityLogPanelView(componentLifetime, project, logModel)
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