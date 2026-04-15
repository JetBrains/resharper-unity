package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.ui.content.ContentFactory
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesCollector
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesDaemon
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.ui.RiderToolWindowFactory
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

/**
 * Tool window for Unity Profiler integration. Content is provided by
 * [UnityProfilerToolContent].
 */
class UnityProfilerToolWindowFactory : RiderToolWindowFactory() {

    companion object {
        const val TOOLWINDOW_ID = "Unity Profiler"

        fun getToolWindow(project: Project): ToolWindow? =
            ToolWindowManager.getInstance(project).getToolWindow(TOOLWINDOW_ID)

        fun makeAvailable(project: Project) {
            UnityProjectLifetimeService.getScope(project).launch(Dispatchers.EDT) {
                val toolWindow = getToolWindow(project) ?: return@launch
                if (!ToolWindowManager.getInstance(project).isStripeButtonShow(toolWindow)){
                    toolWindow.isAvailable = true
                }
            }
        }

        fun show(project: Project) {
            val toolWindow = getToolWindow(project) ?: return
            toolWindow.isAvailable = true
            toolWindow.show()
            UnityProfilerUsagesCollector.logToolWindowOpened(project)
        }

        fun showAndNavigate(project: Project, navigationText: String) {
            val toolWindow = getToolWindow(project) ?: return
            if(!toolWindow.isVisible)
                UnityProfilerUsagesCollector.logToolWindowOpened(project)
            toolWindow.isAvailable = true
            toolWindow.show {
                val daemon = project.service<UnityProfilerUsagesDaemon>()
                daemon.treeViewModel.setFilter(navigationText, true)
            }
        }

        fun activateToolWindowIfNotActive(project: Project) {
            val toolWindow = getToolWindow(project) ?: return
            if (!toolWindow.isActive) toolWindow.activate {}
        }
    }

    override fun shouldBeAvailable(project: Project): Boolean {
        return UnityProjectDiscoverer.getInstance(project).isUnityProject.value
    }

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow, lifetime: Lifetime) {
        toolWindow.isAvailable = true

        val ui = UnityProfilerToolContent(project, lifetime, toolWindow)
        val content = ContentFactory.getInstance().createContent(ui, "", false).apply {
            isCloseable = false
        }
        toolWindow.contentManager.addContent(content)
    }
}

