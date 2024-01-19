package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.ui.ExperimentalUI
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.Deferred

fun AnActionEvent.getFrontendBackendModel(): FrontendBackendModel? {
    val project = project ?: return null
    return project.solution.frontendBackendModel
}

val AnActionEvent.isUnityProject: Deferred<Boolean>?
    get() = project?.isUnityProject

val AnActionEvent.isUnityProjectFolder: Deferred<Boolean>?
    get() = project?.isUnityProjectFolder

fun AnActionEvent.handleUpdateForUnityConnection(update: ((FrontendBackendModel) -> Boolean)? = null) {
    if (!isUnityProject.getCompletedOr(false) && !ExperimentalUI.isNewUI()) { // do not hide UnityActions in the toolbar for the new UI
        presentation.isVisible = false
        return
    }

    presentation.isVisible = true

    val model = getFrontendBackendModel() ?: return
    val updateResult = update?.invoke(model) ?: true
    presentation.isEnabled = project.isConnectedToEditor() && updateResult
}
