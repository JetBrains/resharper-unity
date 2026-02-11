package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.jetbrains.rider.plugins.unity.hasUnityReference
import com.jetbrains.rider.plugins.unity.ui.UnityImportantActions

// need to specify separate classes for each ActionGroup, otherwise RIDER-85088 happens
class UnityToolbarActionsGroup : UnityToolbarActionsGroupBase() {}
class NewUIUnityToolbarActionsGroup : UnityToolbarActionsGroupBase() {}

open class UnityToolbarActionsGroupBase : DefaultActionGroup() {
    override fun getActionUpdateThread(): ActionUpdateThread {
        return ActionUpdateThread.BGT
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project == null) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = (project.hasUnityReference.value
                                    || UnityImportantActions.isVisible(e))
    }
}