package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory

class ShowUnityLogInRiderAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        e.getFrontendBackendModel() ?: return
        UnityToolWindowFactory.show(project)
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = e.project.isConnectedToEditor()
    }
}