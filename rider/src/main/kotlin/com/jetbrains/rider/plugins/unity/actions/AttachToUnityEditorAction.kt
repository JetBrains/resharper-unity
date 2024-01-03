package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityEditor
import com.jetbrains.rider.plugins.unity.run.configurations.isAttachedToUnityEditor

class AttachUnityEditorAction: DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        attachToUnityEditor(project)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        e.presentation.isEnabled = !isAttachedToUnityEditor(project)
    }

    override fun getActionUpdateThread() = ActionUpdateThread.BGT
}