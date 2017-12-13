package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class RefreshInUnityAction(val projectCustomDataHost: ProjectCustomDataHost) : AnAction(UnityIcons.AttachEditorDebugConfiguration) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        projectCustomDataHost.CallBackendRefresh(project)
    }
}