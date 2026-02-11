package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerIntegrationSettingsModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleUnityProfilerAutoFetchAction(private val settings: UnityProfilerIntegrationSettingsModel) : ToggleAction(
    UnityUIBundle.message("unity.profiler.integration.auto.fetch"),
    null,
    AllIcons.General.RefreshAuto
) {
    override fun isSelected(e: AnActionEvent): Boolean = settings.fetchingMode.valueOrNull == FetchingMode.Auto
    override fun setSelected(e: AnActionEvent, state: Boolean) {
        settings.fetchingMode.set(if (state) FetchingMode.Auto else FetchingMode.Manual)
    }

    override fun update(e: AnActionEvent) {
        super.update(e)
        e.presentation.isEnabled = settings.isIntegrationEnabled.valueOrNull ?: false
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
