package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.application

class RefreshInUnityAction() : AnAction("Refresh", "Starts refresh in Unity", UnityIcons.AttachEditorDebugConfiguration) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        application.saveAll()
        ProjectCustomDataHost.CallBackendRefresh(project)
    }
}