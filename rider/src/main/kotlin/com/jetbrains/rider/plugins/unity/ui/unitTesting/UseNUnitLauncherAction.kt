package com.jetbrains.rider.plugins.unity.ui.unitTesting

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnitTestLaunchPreference
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution

class UseNUnitLauncherAction : DumbAwareAction(UseNUnitLauncherActionText,
    UnityUIBundle.message("action.run.with.nunit.launcher.description"), null) {
    companion object {
        val UseNUnitLauncherActionText = UnityUIBundle.message("action.run.with.nunit.launcher.text")
    }
    
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.unitTestPreference.value = UnitTestLaunchPreference.NUnit
    }
}