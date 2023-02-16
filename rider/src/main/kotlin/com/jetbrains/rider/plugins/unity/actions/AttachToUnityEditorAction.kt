package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.attachToUnityEditor
import com.jetbrains.rider.plugins.unity.run.configurations.isAttachedToUnityEditor

class AttachUnityEditorAction: DumbAwareAction() {
    private val logger = Logger.getInstance(AttachUnityEditorAction::class.java)

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        if (!attachToUnityEditor(project)) {
            logger.warn("Have not found run-configuration ${DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME}.")
        }
    }

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        e.presentation.isEnabled = !isAttachedToUnityEditor(project)
    }

    override fun getActionUpdateThread() = ActionUpdateThread.BGT
}