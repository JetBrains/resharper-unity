package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.actions.isUnityProject
import com.jetbrains.rider.plugins.unity.actions.isUnityProjectFolder
import com.jetbrains.rider.projectView.solution
import icons.UnityIcons

class UnityImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        if (!e.isUnityProjectFolder()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true
        e.presentation.icon = UnityIcons.Actions.UnityActionsGroup
    }
}

class UnityDllImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project == null || e.isUnityProject() ||
            !project.solution.rdUnityModel.hasUnityReference.valueOrDefault(false)) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true
        e.presentation.icon = UnityIcons.Actions.UnityActionsGroup
    }
}