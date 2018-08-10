package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.solution

class InstallEditorPluginAction : AnAction("Install EditorPlugin", "Install/Update EditorPlugin.", UnityIcons.Actions.ImportantActions) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.installEditorPlugin.start(Unit)
    }
}