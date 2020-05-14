package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.model.UnitTestLaunchPreference
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.projectView.solution

class UseUnityEditLauncherAction : DumbAwareAction(EditModeDescription, "Run with Unity Editor in Edit Mode", null) {
    companion object {
        const val EditModeDescription = "Unity Editor - Edit Mode"
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.unitTestPreference.value = UnitTestLaunchPreference.EditMode
    }

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        e.presentation.isEnabled = project.isConnectedToEditor()
        e.presentation.isVisible = true
    }
}