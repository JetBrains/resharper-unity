package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution

class UseUnityBothLauncherAction : DumbAwareAction(BothModeText,
    UnityUIBundle.message("action.run.with.unity.editor.description"), null) {
    companion object {
        val BothModeText = UnityUIBundle.message("action.run.with.unity.editor.text")
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.unitTestPreference.value = UnitTestLaunchPreference.Both
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isVisible = true
    }
}