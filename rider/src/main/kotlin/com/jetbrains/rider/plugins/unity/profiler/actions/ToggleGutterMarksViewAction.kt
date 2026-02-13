package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.actions.UnityPluginActionsBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleGutterMarksViewAction(
    private val property: IOptProperty<ProfilerGutterMarkRenderSettings>
) : ToggleAction(
    UnityPluginActionsBundle.message("action.unityProfiler.MinimizeAnnotations.text"),
    null,
    null
) {
    override fun isSelected(e: AnActionEvent): Boolean =
        property.valueOrDefault(ProfilerGutterMarkRenderSettings.Default) == ProfilerGutterMarkRenderSettings.Minimized

    override fun setSelected(e: AnActionEvent, state: Boolean): Unit =
        property.set(if(state) ProfilerGutterMarkRenderSettings.Minimized else ProfilerGutterMarkRenderSettings.Default)

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}