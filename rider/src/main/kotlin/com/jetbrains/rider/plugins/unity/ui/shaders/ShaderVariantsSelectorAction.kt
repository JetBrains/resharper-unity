package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.ui.awt.RelativePoint
import icons.UnityIcons
import java.awt.Point
import java.awt.event.MouseEvent

class ShaderVariantsSelectorAction : DumbAwareAction(UnityIcons.FileTypes.ShaderLab) {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val component = e.inputEvent?.component ?: return
        ShaderVariantsSelector.show(project, RelativePoint(component, (e.inputEvent as? MouseEvent)?.point ?: Point(0, 0)))
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabledAndVisible = e.project != null
    }
}