package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution

class UseUnityPlayLauncherAction : DumbAwareAction(PlayModeDescription,
    UnityUIBundle.message("action.run.with.unity.editor.in.play.mode.description"), null) {
    companion object {
        val PlayModeDescription = UnityUIBundle.message("action.run.with.unity.editor.in.play.mode.text")
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.unitTestPreference.value = UnitTestLaunchPreference.PlayMode
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isVisible = true
    }
}