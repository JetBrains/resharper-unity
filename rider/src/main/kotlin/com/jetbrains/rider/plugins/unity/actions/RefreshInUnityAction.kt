package com.jetbrains.rider.plugins.unity.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.application
import com.jetbrains.rider.util.idea.getComponent

class RefreshInUnityAction(private val unityHost:UnityHost) : AnAction("Refresh", "Starts refresh in Unity", UnityIcons.RefreshInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        application.saveAll()
        UnityHost.CallBackendRefresh(project, true)
    }

    override fun update(e: AnActionEvent) {
    	// TODO: fix after merge
        val projectCustomDataHost = e.getHost() ?: return

        e.presentation.isEnabled = projectCustomDataHost.sessionInitialized.value
        super.update(e)
    }
}