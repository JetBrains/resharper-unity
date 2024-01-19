package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsActions
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class UnityProtocolConnectionAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun actionPerformed(p0: AnActionEvent) {}

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = false

        val project = e.project ?: return
        e.presentation.text = getTooltipText(project)

    }

    @NlsActions.ActionText
    fun getTooltipText(project: Project): String {
        val model = project.solution.frontendBackendModel
        return when (model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected)) {
            UnityEditorState.Disconnected -> UnityBundle.message("action.not.connected.to.unity.editor.text")
            else -> UnityBundle.message("action.connected.to.unity.editor.text")
        }
    }
}