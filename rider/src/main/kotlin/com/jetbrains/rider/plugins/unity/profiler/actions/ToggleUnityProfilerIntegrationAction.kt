package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerIntegrationSettingsModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleUnityProfilerIntegrationAction(private val settings: UnityProfilerIntegrationSettingsModel) : ToggleAction(
    UnityUIBundle.message("unity.profiler.integration.enable"),
    null,
    null
){
    override fun isSelected(e: AnActionEvent): Boolean = settings.isIntegrationEnabled.valueOrNull == true
    override fun setSelected(e: AnActionEvent, state: Boolean): Unit = settings.isIntegrationEnabled.set(state)
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
