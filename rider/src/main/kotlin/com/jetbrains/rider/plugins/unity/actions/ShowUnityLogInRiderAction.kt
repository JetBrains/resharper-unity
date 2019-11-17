package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory

class ShowUnityLogInRiderAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        e.getHost() ?: return
        UnityToolWindowFactory.show(project)
    }

    override fun update(e: AnActionEvent) {
        val host = e.getHost()
        e.presentation.isEnabled = !(host == null || !host.sessionInitialized.valueOrDefault(false))
    }
}