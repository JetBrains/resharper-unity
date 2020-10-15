package com.jetbrains.rider.plugins.unity.actions


import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.isUnityProjectFolder
import com.jetbrains.rider.model.unity.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.projectView.solution

fun AnActionEvent.getModel(): FrontendBackendModel? {
    val project = project ?: return null
    return project.solution.frontendBackendModel
}

fun AnActionEvent.getHost(): UnityHost? {
    val project = project ?: return null
    return UnityHost.getInstance(project)
}

fun AnActionEvent.isUnityProject(): Boolean {
    val project = this.project ?: return false
    return project.isUnityProject()
}

fun AnActionEvent.isUnityProjectFolder(): Boolean {
    val project = this.project ?: return false
    return project.isUnityProjectFolder()
}

fun AnActionEvent.handleUpdateForUnityConnection(fn: ((FrontendBackendModel) -> Boolean)? = null) {
    if (!isUnityProject()) {
        presentation.isVisible = false
        return
    }

    presentation.isVisible = true

    val model = getModel() ?: return
    val connectedProperty = fn?.invoke(model) ?: true
    presentation.isEnabled = connectedProperty && model.unityEditorConnected.valueOrDefault(false)
}
