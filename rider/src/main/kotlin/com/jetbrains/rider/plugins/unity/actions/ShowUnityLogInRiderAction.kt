package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory

class ShowUnityLogInRiderAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        e.getFrontendBackendModel() ?: return
        val context = UnityToolWindowFactory.getInstance(project).getOrCreateContext()
        context.activateToolWindowIfNotActive()
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = e.getFrontendBackendModel() != null
    }
}