package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.projectView.solution

abstract class ProfilerGutterMarksAction : DumbAwareAction() {
    //todo add UnityProfilerUsagesDaemon to get access to the data

    abstract val targetSettings: ProfilerGutterMarkRenderSettings

    // The action is only shown while the gutter marks are in this state (i.e. the state it switches away from).
    abstract val visibleWhen: ProfilerGutterMarkRenderSettings

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun update(e: AnActionEvent) {
        val project = e.project
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isEnabledAndVisible =
            project != null &&
            project.isUnityProject.value &&
            editor != null &&
            gutterMarkRenderSettings(e) == visibleWhen
    }

    override fun actionPerformed(e: AnActionEvent) {
        gutterMarkRenderSettingsProperty(e)?.set(targetSettings)
    }
}

class MinimizeUnityProfilerGutterMarksAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Minimized
    override val visibleWhen: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Default
}

class MaximizeUnityProfilerGutterMarksAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Default
    override val visibleWhen: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Minimized
}

private fun gutterMarkRenderSettings(e: AnActionEvent): ProfilerGutterMarkRenderSettings? =
    gutterMarkRenderSettingsProperty(e)?.valueOrDefault(
        ProfilerGutterMarkRenderSettings.Default
    )

private fun gutterMarkRenderSettingsProperty(e: AnActionEvent): IOptProperty<ProfilerGutterMarkRenderSettings>? =
    e.project?.solution?.frontendBackendModel?.frontendBackendProfilerModel?.gutterMarksRenderSettings