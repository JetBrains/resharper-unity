package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.jetbrains.rider.model.ContextInfo
import com.jetbrains.rider.model.EditableEntityId
import com.jetbrains.rider.model.ShaderContextData
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.ui.shaders.ShaderWidget.Companion.getContextPresentation
import javax.swing.JLabel

class ShaderContextSwitchAction(val project: Project, val id: EditableEntityId, val host: UnityHost,
                                val data: ShaderContextData, val uiLabel: JLabel) : AnAction(data.name) {

    override fun actionPerformed(p0: AnActionEvent) {
        uiLabel.text = getContextPresentation(data)
        host.model.changeContext.fire(ContextInfo(id, data.path, data.start, data.end))
    }

}