package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerIntegrationSettingsModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleUnityProfilerGutterMarksAction(private val settings: UnityProfilerIntegrationSettingsModel) : ToggleAction(
    UnityUIBundle.message("unity.profiler.integration.gutter.marks.enable"),
    null,
    null
) {
    override fun isSelected(e: AnActionEvent): Boolean =
        settings.gutterMarksRenderSettings.valueOrDefault(ProfilerGutterMarkRenderSettings.Default) != ProfilerGutterMarkRenderSettings.Hidden

    override fun setSelected(e: AnActionEvent, state: Boolean): Unit =
        settings.gutterMarksRenderSettings.set(if (state) ProfilerGutterMarkRenderSettings.Default else ProfilerGutterMarkRenderSettings.Hidden)

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
