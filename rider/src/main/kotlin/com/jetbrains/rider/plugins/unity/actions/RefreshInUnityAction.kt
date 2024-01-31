package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.util.application
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class RefreshInUnityAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        application.saveAll()
        project.solution.frontendBackendModel.refresh.fire(true)
    }

    override fun update(e: AnActionEvent) = e.handleUpdateForUnityConnection()
}