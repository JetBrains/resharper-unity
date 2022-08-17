package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.isUnityGeneratedProject

class SwitchUIMode : ToggleAction() {
    override fun isSelected(e: AnActionEvent): Boolean {
        val project = e.project ?: return false
        val uiManager = UnityUIManager.getInstance(project)
        return !uiManager.hasMinimizedUi.hasTrueValue()
    }

    override fun setSelected(e: AnActionEvent, value: Boolean) {
        val project = e.project ?: return
        if(value)
            UnityUIMinimizer.recoverFullUI(project)
        else
            UnityUIMinimizer.ensureMinimizedUI(project)
    }

    override fun update(e: AnActionEvent) {
        // Only enable UI switching for generated Unity projects. Sidecar projects
        // (class library in the main Unity folder) are fairly advanced anyway, so
        // leave things enabled. It also means these projects can access nuget
        e.presentation.isEnabled = e.project?.isUnityGeneratedProject() == true
        super.update(e)
    }
}