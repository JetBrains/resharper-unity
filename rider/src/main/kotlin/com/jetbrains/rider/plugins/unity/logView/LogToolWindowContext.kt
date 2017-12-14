package com.jetbrains.rider.plugins.unity.logView

import com.intellij.openapi.wm.ToolWindow
import com.intellij.ui.content.Content
import com.jetbrains.rider.plugins.unity.RdLogEventType
import com.jetbrains.rider.plugins.unity.logView.ui.LogPanel
import com.jetbrains.rider.util.idea.getLogger

class LogToolWindowContext(private val toolWindow: ToolWindow,
                             private val content: Content,
                             private val panel: LogPanel) {
    companion object {
        private val myLogger = getLogger<LogToolWindowContext>()
    }

    val isActive get() = toolWindow.isActive

    //fun addBuildEvent(buildEvent: BuildEvent) = panel.addBuildEvent(buildEvent)

    fun addOutputMessage(message: String, RdLogEventType: RdLogEventType) = panel.addOutputMessage(message, RdLogEventType)

    fun clear() {
        panel.clearConsole()
        //panel.clearTree()
        panel.showConsole()
    }

//    fun updateProgress(operation: BuildTargetBase) {
//        val status = operation.toProgressText()
//        content.toolwindowTitle = status
//        content.displayName = status
//    }

//    fun updateStatus(kind: BuildResultKind, buildTargetName: String): String {
//        val statusText = kind.toStatusText(buildTargetName)
//        val statusShortText = kind.toShortStatusText(buildTargetName)
//        content.toolwindowTitle = " - $statusShortText"
//        content.displayName = " - $statusShortText"
//        myLogger.info("Build result: $statusShortText - $statusText")
//        //panel.addStatusMessage(statusText)
//        return statusText
//    }

//    fun invalidatePanelMode() {
//        if (panel.hasEvents) {
//            panel.showEvents()
//        }
//    }

    fun activateToolWindowIfNotActive() {
        if (!(toolWindow.isActive)) {
            toolWindow.activate {}
        }
    }
}