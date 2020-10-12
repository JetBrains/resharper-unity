package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.RdDocumentId
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rider.model.unity.frontendBackend.ContextInfo
import com.jetbrains.rider.model.unity.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.UnityHost

class ShaderContextSwitchAction(val project: Project, val id: RdDocumentId, val host: UnityHost,
                                val data: ShaderContextData, private val currentContext:  IProperty<ShaderContextData?>) : AnAction(data.name) {

    override fun actionPerformed(p0: AnActionEvent) {
        currentContext.value = data
        host.model.changeContext.fire(ContextInfo(id, data.path, data.start, data.end))
    }
}

class ShaderAutoContextSwitchAction(val project: Project, val id: RdDocumentId, val host: UnityHost,
                                    private val currentContext:  IProperty<ShaderContextData?>)  : AnAction("Auto") {

    override fun actionPerformed(p0: AnActionEvent) {
        currentContext.value = null
        host.model.setAutoShaderContext.fire(id)
    }
}