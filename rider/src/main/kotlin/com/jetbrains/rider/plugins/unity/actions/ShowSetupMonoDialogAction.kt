package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.util.SystemInfo
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution

open class ShowSetupMonoDialogAction : DumbAwareAction("Show setup mono dialog") {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val dialog = com.jetbrains.rider.environmentSetup.EnvironmentSetupDialog(project, "mono")
        dialog.showAndGet()
    }

    override fun update(e: AnActionEvent) {
        if (SystemInfo.isWindows)
            e.presentation.isVisible = false

        val project = e.project
        if (project == null) {
            e.presentation.isEnabled = false
            return
        }

        super.update(e)
    }
}