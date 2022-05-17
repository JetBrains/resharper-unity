package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityImportantActions
import com.jetbrains.rider.projectView.solution

class UnityToolbarActionsGroup : DefaultActionGroup() {

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        e.presentation.isVisible = (project.solution.frontendBackendModel.hasUnityReference.valueOrDefault(false)
                || UnityImportantActions.isVisible(e))
    }
}