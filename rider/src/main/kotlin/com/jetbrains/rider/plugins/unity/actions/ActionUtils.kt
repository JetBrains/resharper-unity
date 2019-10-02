package com.jetbrains.rider.plugins.unity.actions


import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.UnityHost

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

fun AnActionEvent.handleUpdateForUnityConnection(fn: ((UnityHost) -> Boolean)? = null) {
    if (!isUnityProject()) {
        presentation.isVisible = false
        return
    }

    presentation.isVisible = true

    val host = getHost() ?: return
    val connectedProperty = fn?.invoke(host) ?: true
    presentation.isEnabled = connectedProperty && host.sessionInitialized.valueOrDefault(false)
}
