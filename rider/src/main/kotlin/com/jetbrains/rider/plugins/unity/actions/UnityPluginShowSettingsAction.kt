package com.jetbrains.rider.plugins.unity.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.wm.ToolWindowManager
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.plugins.unity.toolWindow.log.UnityLogPanelIcons

class UnityPluginShowSettingsAction : DumbAwareAction("Unity Plugin Settings...", "", AllIcons.General.Settings) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        ShowSettingsUtil.getInstance().showSettingsDialog(project, "Unity Engine")
    }
}

class ShowUnityLogInRiderAction : DumbAwareAction("Show Unity Editor Log") {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        e.getHost() ?: return
        val toolWindow = ToolWindowManager.getInstance(project)?.getToolWindow(UnityToolWindowFactory.TOOLWINDOW_ID) ?: return
        toolWindow.show {  }
    }

    override fun update(e: AnActionEvent) {
        val host = e.getHost()
        e.presentation.isEnabled = !(host == null || !host.sessionInitialized.value)
    }
}