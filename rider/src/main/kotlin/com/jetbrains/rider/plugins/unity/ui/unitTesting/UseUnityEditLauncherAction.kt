package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution

class UseUnityEditLauncherAction : DumbAwareAction(EditModeText,
    UnityUIBundle.message("action.run.with.unity.editor.in.edit.mode.description"), null) {
    companion object {
        val EditModeText = UnityUIBundle.message("action.run.with.unity.editor.in.edit.mode.text")
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.unitTestPreference.value = UnitTestLaunchPreference.EditMode
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isVisible = true
    }
}