package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.util.NlsActions
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.actions.isUnityProjectFolder
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
import com.jetbrains.rider.projectView.solution
import icons.UnityIcons
import javax.swing.Icon

class UnityImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        if (isVisible(e)) {
            e.presentation.isVisible = true
            e.presentation.icon = getIcon(e)
        } else{
            e.presentation.isVisible = false
        }
    }

    @NlsActions.ActionText
    fun getIcon(e: AnActionEvent): Icon {
        val project = e.project ?: return UnityIcons.Toolbar.Toolbar
        val host = FrontendBackendHost.getInstance(project)
        return when (host.model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected)) {
            UnityEditorState.Disconnected -> UnityIcons.Toolbar.ToolbarDisconnected
            else -> UnityIcons.Toolbar.ToolbarConnected
        }
    }

    companion object{
        fun isVisible(e: AnActionEvent): Boolean {
            return e.isUnityProjectFolder()
        }
    }
}

class UnityDllImportantActions : DefaultActionGroup(), DumbAware {
    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        if (project.solution.frontendBackendModel.hasUnityReference.valueOrDefault(false)
            && !UnityImportantActions.isVisible(e)) {
            e.presentation.isVisible = true
            e.presentation.icon = UnityIcons.Toolbar.Toolbar
        } else{
            e.presentation.isVisible = false

        }
    }
}