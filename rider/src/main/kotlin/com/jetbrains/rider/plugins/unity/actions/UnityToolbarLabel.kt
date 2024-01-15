package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ex.ToolbarLabelAction
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.ui.UnityUIManager
import com.jetbrains.rider.plugins.unity.ui.hasTrueValue

class UnityToolbarLabel : ToolbarLabelAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project == null) {
            e.presentation.isVisible = false
            return
        }
        if (!project.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        if (UnityUIManager.getInstance(project).hasHiddenPlayButtons.hasTrueValue()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true

        super.update(e)
    }
}