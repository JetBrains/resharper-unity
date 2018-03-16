package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.wm.ToolWindow
import com.jetbrains.rider.plugins.unity.editorPlugin.model.*
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelModel

class UnityToolWindowContext(private val toolWindow: ToolWindow,
                             private val logModel: UnityLogPanelModel) {

    fun activateToolWindowIfNotActive() {
        if (!(toolWindow.isActive)) {
            toolWindow.activate {}
        }
    }

    val isActive get() = toolWindow.isActive

    fun addEvent(event: RdLogEvent) = logModel.events.addEvent(event)

    fun clear() = logModel.events.clear()
}