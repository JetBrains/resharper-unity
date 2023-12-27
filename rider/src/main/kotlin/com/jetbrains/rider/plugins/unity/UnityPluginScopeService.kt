package com.jetbrains.rider.plugins.unity

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import kotlinx.coroutines.CoroutineScope

@Service(Service.Level.PROJECT)
class UnityPluginScopeService(val scope: CoroutineScope) {
    companion object {
        fun getScope(project: Project) = project.service<UnityPluginScopeService>().scope
        fun getScope() = service<UnityPluginApplicationScopeService>().scope
    }
}

@Service(Service.Level.APP)
class UnityPluginApplicationScopeService(val scope: CoroutineScope)