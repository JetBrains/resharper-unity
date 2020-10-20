package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.isUnityProjectFolder
import com.jetbrains.rider.model.unity.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.projectView.solution

fun AnActionEvent.getFrontendBackendModel(): FrontendBackendModel? {
    val project = project ?: return null
    return project.solution.frontendBackendModel
}

fun AnActionEvent.isUnityProject(): Boolean {
    val project = this.project ?: return false
    return project.isUnityProject()
}

fun AnActionEvent.isUnityProjectFolder(): Boolean {
    val project = this.project ?: return false
    return project.isUnityProjectFolder()
}

fun AnActionEvent.handleUpdateForUnityConnection(update: ((FrontendBackendModel) -> Boolean)? = null) {
    if (!isUnityProject()) {
        presentation.isVisible = false
        return
    }

    presentation.isVisible = true

    val model = getFrontendBackendModel() ?: return
    val updateResult = update?.invoke(model) ?: true
    presentation.isEnabled = project.isConnectedToEditor() && updateResult
}
