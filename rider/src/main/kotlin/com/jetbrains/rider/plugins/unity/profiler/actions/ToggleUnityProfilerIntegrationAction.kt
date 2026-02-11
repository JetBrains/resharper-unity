package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerIntegrationSettingsModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleUnityProfilerIntegrationAction(private val settings: UnityProfilerIntegrationSettingsModel) : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val currentValue = settings.isIntegrationEnabled.valueOrNull ?: false
        settings.isIntegrationEnabled.set(!currentValue)
    }

    override fun update(e: AnActionEvent) {
        val enabled = settings.isIntegrationEnabled.valueOrNull ?: false
        e.presentation.text = if (enabled) {
            UnityUIBundle.message("unity.profiler.integration.disable")
        } else {
            UnityUIBundle.message("unity.profiler.integration.enable")
        }
        e.presentation.icon = if (enabled) AllIcons.Actions.Pause else AllIcons.Actions.Execute
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
