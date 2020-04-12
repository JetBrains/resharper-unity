package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution

class RefreshInUnityAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        application.saveAll()
        project.solution.rdUnityModel.refresh.fire(true)
    }

    override fun update(e: AnActionEvent) = e.handleUpdateForUnityConnection()
}