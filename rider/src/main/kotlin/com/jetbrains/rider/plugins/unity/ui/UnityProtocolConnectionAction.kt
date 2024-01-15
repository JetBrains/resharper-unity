package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.util.NlsActions
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.UnityEditorState

class UnityProtocolConnectionAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun actionPerformed(p0: AnActionEvent) {}

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = false

        val project = e.project ?: return
        val host = FrontendBackendHost.getInstance(project)

        e.presentation.text = getTooltipText(host)

    }

    @NlsActions.ActionText
    fun getTooltipText(host: FrontendBackendHost): String {
        return when (host.model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected)) {
            UnityEditorState.Disconnected -> UnityBundle.message("action.not.connected.to.unity.editor.text")
            else -> UnityBundle.message("action.connected.to.unity.editor.text")
        }
    }
}