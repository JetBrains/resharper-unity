package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.actions.UnityPluginActionsBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.projectView.solution

abstract class ProfilerGutterMarksAction : DumbAwareAction() {
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

class MinimizeUnityProfilerGutterMarksWithIconAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Minimized

    init {
        templatePresentation.text = UnityPluginActionsBundle.message("action.unityProfiler.MinimizeAnnotations.text")
        templatePresentation.description = UnityPluginActionsBundle.message("action.unityProfiler.MinimizeAnnotations.description")
        templatePresentation.icon = AllIcons.General.CollapseComponent
    }
}

class MaximizeUnityProfilerGutterMarksWithIconAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Default

    init {
        templatePresentation.text = UnityPluginActionsBundle.message("action.unityProfiler.MaximizeAnnotations.text")
        templatePresentation.description = UnityPluginActionsBundle.message("action.unityProfiler.MaximizeAnnotations.description")
        templatePresentation.icon = AllIcons.General.ExpandComponent
    }
}

class HideUnityProfilerGutterMarksWithIconAction : ProfilerGutterMarksAction() {
    override val targetSettings: ProfilerGutterMarkRenderSettings = ProfilerGutterMarkRenderSettings.Hidden

    init {
        templatePresentation.text = UnityPluginActionsBundle.message("action.unityProfiler.HideAnnotations.text")
        templatePresentation.description = UnityPluginActionsBundle.message("action.unityProfiler.HideAnnotations.description")
        templatePresentation.icon = AllIcons.Actions.Cancel
    }
}
private fun gutterMarkRenderSettings(e: AnActionEvent): ProfilerGutterMarkRenderSettings? =
    gutterMarkRenderSettingsProperty(e)?.valueOrDefault(
        ProfilerGutterMarkRenderSettings.Default
    )

private fun gutterMarkRenderSettingsProperty(e: AnActionEvent): IOptProperty<ProfilerGutterMarkRenderSettings>? =
    e.project?.solution?.frontendBackendModel?.frontendBackendProfilerModel?.gutterMarksRenderSettings