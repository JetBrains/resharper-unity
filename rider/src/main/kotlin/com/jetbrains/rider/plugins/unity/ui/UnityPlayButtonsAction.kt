package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.isUnityProject

class UnityPlayButtonsAction : ToggleAction() {
    override fun getActionUpdateThread(): ActionUpdateThread {
        return ActionUpdateThread.BGT
    }

    override fun isSelected(e: AnActionEvent): Boolean {
        val project = e.project ?: return false
        val uiManager = UnityUIManager.getInstance(project)
        return !uiManager.hasHiddenPlayButtons.hasTrueValue()
    }

    override fun setSelected(e: AnActionEvent, value: Boolean) {
        val project = e.project ?: return
        val uiManager = UnityUIManager.getInstance(project)
        uiManager.hasHiddenPlayButtons.value = !value
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = e.project?.isUnityProject() == true
        super.update(e)
    }
}
