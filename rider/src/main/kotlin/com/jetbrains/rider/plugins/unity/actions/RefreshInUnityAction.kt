package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.application

class RefreshInUnityAction(val projectCustomDataHost:ProjectCustomDataHost) : AnAction("Refresh", "Starts refresh in Unity", UnityIcons.RefreshInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        application.saveAll()
        ProjectCustomDataHost.CallBackendRefresh(project, true)
    }

    override fun update(e: AnActionEvent?) {
        e?.presentation?.isEnabled = projectCustomDataHost.isConnected.value
        super.update(e)
    }
}