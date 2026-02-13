package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ToggleUnityProfilerGutterMarksAction(
    private val property: IOptProperty<Boolean>
) : ToggleAction(
    UnityUIBundle.message("unity.profiler.integration.gutter.marks.enable"),
    null,
    null
) {
    override fun isSelected(e: AnActionEvent): Boolean =
        property.valueOrDefault(false)

    override fun setSelected(e: AnActionEvent, state: Boolean): Unit =
        property.set(state)

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
