package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.ide.impl.ContentManagerWatcher
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.impl.status.StatusBarUtil
import com.intellij.ui.content.ContentManagerEvent
import com.intellij.ui.content.ContentManagerListener
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelView
import icons.UnityIcons

//there's an API for registering tool windows in the IJ Platform
class UnityToolWindowFactory(private val project: Project) : LifetimedService() {

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

    // TODO: Use ToolWindowFactory and toolWindow extension points
    @Suppress("DEPRECATION")
    private fun create(): UnityToolWindowContext {
        val toolWindow = ToolWindowManager.getInstance(project).registerToolWindow(TOOL_WINDOW_ID, true, ToolWindowAnchor.BOTTOM, project, true, false)

        val contentManager = toolWindow.contentManager
        contentManager.addContentManagerListener(object : ContentManagerListener {
            override fun selectionChanged(p0: ContentManagerEvent) {
            }

            override fun contentRemoveQuery(p0: ContentManagerEvent) {
            }

            override fun contentAdded(p0: ContentManagerEvent) {
            }

            override fun contentRemoved(event: ContentManagerEvent) {
                context = null
                ToolWindowManager.getInstance(project).unregisterToolWindow(TOOL_WINDOW_ID)
            }
        })
        toolWindow.title = ""
        toolWindow.setIcon(UnityIcons.ToolWindows.UnityLog)
        // Required for hiding window without content
        ContentManagerWatcher(toolWindow, contentManager)

        val logModel = UnityLogPanelModel(project.lifetime, project, toolWindow)
        val logView = UnityLogPanelView(project.lifetime, project, logModel, toolWindow)
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