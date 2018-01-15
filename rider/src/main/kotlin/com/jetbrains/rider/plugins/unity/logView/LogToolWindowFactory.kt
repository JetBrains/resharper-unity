package com.jetbrains.rider.plugins.unity.logView

import com.intellij.ide.CommonActionsManager
import com.intellij.ide.actions.CloseTabToolbarAction
import com.intellij.ide.actions.ContextHelpAction
import com.intellij.ide.impl.ContentManagerWatcher
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.components.AbstractProjectComponent
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.IconLoader
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager
import com.intellij.openapi.wm.impl.status.StatusBarUtil
import com.intellij.ui.content.ContentManager
import com.intellij.ui.content.ContentManagerAdapter
import com.intellij.ui.content.ContentManagerEvent
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.actions.*
import com.jetbrains.rider.plugins.unity.logView.ui.LogPanel
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.util.idea.ILifetimedComponent
import com.jetbrains.rider.util.idea.LifetimedComponent
import java.awt.BorderLayout
import javax.swing.JPanel

class LogToolWindowFactory(private val project: Project,
                           private val toolWindowManager: ToolWindowManager,
                           private val actionManager: CommonActionsManager,
                           private val projectModelViewHost: ProjectModelViewHost)
    : AbstractProjectComponent(project), ILifetimedComponent by LifetimedComponent(project) {
    companion object {
        val TOOLWINDOW_ID = "UnityLog"
        private val HELP_ID = "unityLog.toolWindow"
    }

    private val lock = Object()

    private var context: LogToolWindowContext? = null

    fun getOrCreateContext(): LogToolWindowContext {
        synchronized(lock) {
            return context ?: create()
        }
    }

    private fun create(): LogToolWindowContext {
        val toolWindow = toolWindowManager.registerToolWindow(TOOLWINDOW_ID, true, ToolWindowAnchor.BOTTOM, project, true, false)
        val contentManager = toolWindow.contentManager
        contentManager.addContentManagerListener(object : ContentManagerAdapter() {
            override fun contentRemoved(event: ContentManagerEvent?) {
                context = null
                toolWindowManager.unregisterToolWindow(TOOLWINDOW_ID)
            }
        })
        toolWindow.title = ""
        toolWindow.icon = IconLoader.getIcon("/rider/toolwindows/toolWindowBuild.png")
        // Required for hiding window without content
        ContentManagerWatcher(toolWindow, contentManager)
        val panel = LogPanel(project, projectModelViewHost, componentLifetime)
        val toolWindowContent = contentManager.factory.createContent(null, "Unity Console", true).apply {
            StatusBarUtil.setStatusBarInfo(project, "")
            component = panel
            panel.setToolbar(createToolbarPanel(panel, contentManager))
            isCloseable = false
        }
        contentManager.addContent(toolWindowContent)
        val ctx = LogToolWindowContext(toolWindow, toolWindowContent, panel)
        context = ctx
        return ctx
    }

    private fun createToolbarPanel(buildResultPanel: LogPanel, contentManager: ContentManager): JPanel {
        val buildActionGroup = DefaultActionGroup().apply {
            add(RefreshInUnityAction())
            add(PlayInUnityAction())
            add(PauseInUnityAction())
            add(ResumeInUnityAction())
            add(StopInUnityAction())
            add(actionManager.createPrevOccurenceAction(buildResultPanel))
            add(actionManager.createNextOccurenceAction(buildResultPanel))
            addSeparator()
            //add(RiderBuildShowSettingsAction(project))
            add(ContextHelpAction(HELP_ID))
            add(object : CloseTabToolbarAction() {
                override fun update(e: AnActionEvent) {
                    //e.presentation.isEnabled = !buildHost.building.value
                }

                override fun actionPerformed(e: AnActionEvent) {
//                    if (buildHost.building.value)
//                        return
//                    contentManager.removeAllContents(true)
                }
            })
        }
        return JPanel(BorderLayout()).apply {
            add(ActionManager.getInstance().createActionToolbar(ActionPlaces.COMPILER_MESSAGES_TOOLBAR, buildActionGroup, false).component, BorderLayout.WEST)
        }
    }
}