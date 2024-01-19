package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ex.TooltipDescriptionProvider
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.actions.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.hasUnityReference
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import icons.UnityIcons

class UnityImportantActions : DefaultActionGroup(), DumbAware, TooltipDescriptionProvider {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project != null && isVisible(e)) {
            e.presentation.isVisible = true

            val model = project.solution.frontendBackendModel
            when (model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected)) {
                UnityEditorState.Disconnected -> {
                    e.presentation.icon = UnityIcons.Toolbar.ToolbarDisconnected
                    e.presentation.text = UnityUIBundle.message("action.not.connected.to.unity.editor.text")
                    e.presentation.description = UnityUIBundle.message("action.not.connected.to.unity.editor.description")
                }

                else -> {
                    e.presentation.icon = UnityIcons.Toolbar.ToolbarConnected
                    e.presentation.text = UnityUIBundle.message("action.connected.to.unity.editor.text")
                    e.presentation.description = null
                }
            }
        } else {
            e.presentation.isVisible = false
        }
    }

    companion object{
        fun isVisible(e: AnActionEvent) = e.isUnityProjectFolder.getCompletedOr(false)
    }
}

class UnityDllImportantActions : DefaultActionGroup(), DumbAware {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        if (project.hasUnityReference.getCompletedOr(false) && !UnityImportantActions.isVisible(e)
        ) {
            e.presentation.isVisible = true
            e.presentation.icon = UnityIcons.Toolbar.Toolbar
        } else {
            e.presentation.isVisible = false
        }
    }
}