package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.actions.isUnityProject
import com.jetbrains.rider.plugins.unity.actions.isUnityProjectFolder
import com.jetbrains.rider.projectView.solution
import icons.UnityIcons

class UnityImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        if (isVisible(e)) {
            e.presentation.isVisible = true
            e.presentation.icon = UnityIcons.Actions.UnityActionsGroup
        } else{
            e.presentation.isVisible = false
        }
    }

    companion object{
        fun isVisible(e: AnActionEvent): Boolean {
            return e.isUnityProjectFolder()
        }
    }
}

class UnityDllImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        if (project.solution.frontendBackendModel.hasUnityReference.valueOrDefault(false)
            && !UnityImportantActions.isVisible(e)) {
            e.presentation.isVisible = true
            e.presentation.icon = UnityIcons.Actions.UnityActionsGroup
        } else{
            e.presentation.isVisible = false

        }
    }
}