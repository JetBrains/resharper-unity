package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.solution

open class InstallEditorPluginAction : DumbAwareAction("Install Unity Editor plugin", "Install/Update Unity Editor plugin.", UnityIcons.Actions.ImportantActions) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        //project.solution.rdUnityModel.installEditorPlugin.fire(Unit)
        UnityHost.CallBackendInstallEditorPlugin(project)
    }
}