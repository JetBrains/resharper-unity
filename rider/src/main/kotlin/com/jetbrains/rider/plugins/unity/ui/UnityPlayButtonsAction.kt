package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rider.plugins.unity.isUnityGeneratedProject

class UnityPlayButtonsAction : AnAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val uiManager = UnityUIManager.getInstance(project)

        val currentValue = uiManager.hasHiddenPlayButtons.hasTrueValue()
        uiManager.hasHiddenPlayButtons.value = !currentValue
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project == null) {
            e.presentation.isEnabled = false
            return
        }

        if(!project.isUnityGeneratedProject()) {
            e.presentation.isEnabled = false
            return
        }

        if(UnityUIManager.getInstance(project).hasHiddenPlayButtons.hasTrueValue())
            e.presentation.text = "Show Unity Play/Pause Actions"
        else
            e.presentation.text = "Hide Unity Play/Pause Actions"

        super.update(e)
    }
}
