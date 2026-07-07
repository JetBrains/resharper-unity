package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.projectView.solution

abstract class ProfilerGutterMarksAction : DumbAwareAction() {
    //todo add UnityProfilerUsagesDaemon to get access to the data
    
    abstract val targetSettings: ProfilerGutterMarkRenderSettings

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        gutterMarkRenderSettingsProperty(e)?.set(targetSettings)
    }
}

class MinimizeUnityProfilerGutterMarksAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Minimized

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = true
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isVisible =
            editor != null && gutterMarkRenderSettings(e) == ProfilerGutterMarkRenderSettings.Default
    }
}

class MaximizeUnityProfilerGutterMarksAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Default

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = true
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isVisible =
            editor != null && gutterMarkRenderSettings(e) == ProfilerGutterMarkRenderSettings.Minimized
    }
}

private fun gutterMarkRenderSettings(e: AnActionEvent): ProfilerGutterMarkRenderSettings? =
    gutterMarkRenderSettingsProperty(e)?.valueOrDefault(
        ProfilerGutterMarkRenderSettings.Default
    )

private fun gutterMarkRenderSettingsProperty(e: AnActionEvent): IOptProperty<ProfilerGutterMarkRenderSettings>? =
    e.project?.solution?.frontendBackendModel?.frontendBackendProfilerModel?.gutterMarksRenderSettings