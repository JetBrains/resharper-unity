package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rider.plugins.unity.actions.isUnityProject
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class UnityImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        if (!e.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true
        e.presentation.icon = UnityIcons.Actions.UnityActionsGroup
    }
}