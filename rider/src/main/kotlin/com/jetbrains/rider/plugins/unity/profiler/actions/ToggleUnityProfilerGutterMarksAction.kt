package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerIntegrationSettingsModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleUnityProfilerGutterMarksAction(private val settings: UnityProfilerIntegrationSettingsModel) : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val currentValue = settings.gutterMarksRenderSettings.valueOrNull ?: ProfilerGutterMarkRenderSettings.Default
        if (currentValue == ProfilerGutterMarkRenderSettings.Hidden) {
            settings.gutterMarksRenderSettings.set(ProfilerGutterMarkRenderSettings.Default)
        } else {
            settings.gutterMarksRenderSettings.set(ProfilerGutterMarkRenderSettings.Hidden)
        }
    }

    override fun update(e: AnActionEvent) {
        val current = settings.gutterMarksRenderSettings.valueOrNull ?: ProfilerGutterMarkRenderSettings.Default
        val isHidden = current == ProfilerGutterMarkRenderSettings.Hidden
        e.presentation.text = if (isHidden) {
            UnityUIBundle.message("unity.profiler.integration.gutter.marks.enable")
        } else {
            UnityUIBundle.message("unity.profiler.integration.gutter.marks.disable")
        }
        e.presentation.icon = if (isHidden) AllIcons.Actions.Show else AllIcons.Actions.ToggleVisibility
        e.presentation.isEnabled = settings.isIntegrationEnabled.valueOrNull ?: false
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
