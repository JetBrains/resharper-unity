package com.jetbrains.rider.plugins.unity.actions

import com.intellij.execution.Executor
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionUtil
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType

class AttachUnityEditorAction: DumbAwareAction() {
    private val logger = Logger.getInstance(AttachUnityEditorAction::class.java)

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        val runManager = RunManager.getInstance(project)
        val settings = runManager.findConfigurationByTypeAndName(
                UnityDebugConfigurationType.id, DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME)

        if (settings != null) {
            ExecutionUtil.runConfiguration(settings,
                Executor.EXECUTOR_EXTENSION_NAME.extensionList.single { it is DefaultDebugExecutor })
        } else {
            logger.warn("Have not found run-configuration ${DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME}.")
        }
    }
}