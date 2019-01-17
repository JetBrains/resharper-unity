package com.jetbrains.rider.plugins.unity.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.util.idea.application

class RefreshInUnityAction : AnAction("Refresh", "Triggers Refresh in Unity Editor", AllIcons.Actions.Refresh) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        application.saveAll()
        project.solution.rdUnityModel.refresh.fire(true)
    }

    override fun update(e: AnActionEvent) {
        if (!e.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true

        val projectCustomDataHost = e.getHost() ?: return
        e.presentation.isEnabled = projectCustomDataHost.sessionInitialized.valueOrDefault(false)
        super.update(e)
    }
}