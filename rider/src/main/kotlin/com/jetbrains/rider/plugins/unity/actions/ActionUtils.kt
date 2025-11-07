package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.ui.ExperimentalUI
import com.jetbrains.rd.framework.impl.RdProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

fun AnActionEvent.getFrontendBackendModel(): FrontendBackendModel? {
    val project = project ?: return null
    return project.solution.frontendBackendModel
}

val AnActionEvent.isUnityProject: Property<Boolean>?
    get() = project?.isUnityProject

val AnActionEvent.isUnityProjectFolder: RdProperty<Boolean>?
    get() = project?.isUnityProjectFolder

val RdProperty<Boolean>?.valueOrDefault: Boolean
    get() {
        if (this == null) return false
        return this.value
    }

val Property<Boolean>?.valueOrDefault: Boolean
    get() {
        if (this == null) return false
        return this.value
    }

fun AnActionEvent.handleUpdateForUnityConnection(update: ((FrontendBackendModel) -> Boolean)? = null) {
    if (!isUnityProject.valueOrDefault && !ExperimentalUI.isNewUI()) { // do not hide UnityActions in the toolbar for the new UI
        presentation.isVisible = false
        return
    }

    presentation.isVisible = true

    val model = getFrontendBackendModel() ?: return
    val updateResult = update?.invoke(model) ?: true
    presentation.isEnabled = project.isConnectedToEditor() && updateResult
}
