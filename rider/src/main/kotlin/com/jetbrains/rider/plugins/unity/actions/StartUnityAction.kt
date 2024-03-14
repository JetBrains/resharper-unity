package com.jetbrains.rider.plugins.unity.actions

import com.intellij.execution.ExecutionTargetManager
import com.intellij.execution.Executor
import com.intellij.execution.RunManager
import com.intellij.execution.executors.DefaultRunExecutor
import com.intellij.execution.runners.ExecutionUtil
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.util.getUnityArgs
import com.jetbrains.rider.plugins.unity.util.withProjectPath
import com.jetbrains.rider.plugins.unity.util.withRiderPath
import com.jetbrains.rider.projectView.solution


open class StartUnityAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        startUnity(project)
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
    override fun update(e: AnActionEvent) {
        val model = e.project?.solution?.frontendBackendModel

        @NlsSafe
        val version = model?.unityApplicationData?.valueOrNull?.applicationVersion

        if (version != null)
            e.presentation.text = UnityPluginActionsBundle.message("action.start.unity.text", version)

        e.presentation.isEnabled = version != null && !e.project.isConnectedToEditor()
        super.update(e)
    }

    companion object {
        private val logger = Logger.getInstance(StartUnityAction::class.java)
        fun startUnity(project: Project) {
            val runManager = RunManager.getInstance(project)
            val settings =
                runManager.findConfigurationByTypeAndName(UnityExeConfigurationType.id,
                                                          DefaultRunConfigurationGenerator.RUN_DEBUG_START_UNITY_CONFIGURATION_NAME)

            if (settings?.configuration != null && ExecutionTargetManager.getInstance(project).getTargetsFor(
                    settings.configuration).isEmpty()) {
                ExecutionUtil.runConfiguration(settings,
                                               Executor.EXECUTOR_EXTENSION_NAME.extensionList.single { it is DefaultRunExecutor && it.id == DefaultRunExecutor.EXECUTOR_ID })
            }
            else {
                logger.warn(
                    "UnityExeConfiguration ${DefaultRunConfigurationGenerator.RUN_DEBUG_START_UNITY_CONFIGURATION_NAME} was not found.")
                val processBuilderArgs = getUnityArgs(project).withProjectPath(project).withRiderPath()
                startUnity(processBuilderArgs)
            }
        }

        fun startUnity(args: MutableList<String>): Process? {
            val processBuilder = ProcessBuilder(args)
            // only needed for locally compiled Rider, which can contaminate Unity/UnityHub with this env
            processBuilder.environment().remove("RESHARPER_HOST_BIN")
            return processBuilder.start()
        }
    }
}