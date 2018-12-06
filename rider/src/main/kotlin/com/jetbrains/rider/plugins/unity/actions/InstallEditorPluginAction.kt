package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution

open class InstallEditorPluginAction : DumbAwareAction("Install UnityEditor plugin") {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.installEditorPlugin.fire(Unit)
    }
}