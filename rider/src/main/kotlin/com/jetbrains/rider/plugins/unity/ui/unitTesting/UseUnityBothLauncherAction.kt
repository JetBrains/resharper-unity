package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.model.unity.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class UseUnityBothLauncherAction : DumbAwareAction(BothModeDescription, "Run with Unity Editor", null) {
    companion object {
        const val BothModeDescription = "Unity - Edit && Play Mode"
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.unitTestPreference.value = UnitTestLaunchPreference.Both
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isVisible = true
    }
}