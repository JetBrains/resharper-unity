package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.model.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.application

class RefreshInUnityAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        application.saveAll()
        project.solution.frontendBackendModel.refresh.fire(true)
    }

    override fun update(e: AnActionEvent) = e.handleUpdateForUnityConnection()
}