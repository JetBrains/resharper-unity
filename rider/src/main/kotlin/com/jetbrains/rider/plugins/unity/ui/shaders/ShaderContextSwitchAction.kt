package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rider.model.ContextInfo
import com.jetbrains.rider.model.EditableEntityId
import com.jetbrains.rider.model.ShaderContextData
import com.jetbrains.rider.plugins.unity.UnityHost

class ShaderContextSwitchAction(val project: Project, val id: EditableEntityId, val host: UnityHost,
                                val data: ShaderContextData, val currentContext:  IProperty<ShaderContextData?>) : AnAction(data.name) {

    override fun actionPerformed(p0: AnActionEvent) {
        currentContext.value = data
        host.model.changeContext.fire(ContextInfo(id, data.path, data.start, data.end))
    }
}

class ShaderAutoContextSwitchAction(val project: Project, val id: EditableEntityId, val host: UnityHost,
                                val currentContext:  IProperty<ShaderContextData?>)  : AnAction("Auto") {

    override fun actionPerformed(p0: AnActionEvent) {
        currentContext.value = null
        host.model.setAutoShaderContext.fire(id)
    }
}