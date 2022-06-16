package com.jetbrains.rider.plugins.unity.actions

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.DefaultRunConfigurationGenerator
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfigurationType
import com.jetbrains.rider.plugins.unity.util.getUnityArgs
import com.jetbrains.rider.plugins.unity.util.withProjectPath
import com.jetbrains.rider.plugins.unity.util.withRiderPath
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.createEmptyConsoleCommandLine
import com.jetbrains.rider.run.withRawParameters


open class StartUnityAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return

        startUnity(project)
    }

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
        fun startUnity(project: Project): Process? {
            val runManager = RunManager.getInstance(project)
            val settings =
                runManager.findConfigurationByTypeAndName(UnityExeConfigurationType.id, DefaultRunConfigurationGenerator.RUN_DEBUG_START_UNITY_CONFIGURATION_NAME)

            if (settings != null){
                val exeConfiguration = settings.configuration as UnityExeConfiguration
                val runCommandLine = createEmptyConsoleCommandLine(exeConfiguration.parameters.useExternalConsole)
                    .withEnvironment(exeConfiguration.parameters.envs)
                    .withParentEnvironmentType(if (exeConfiguration.parameters.isPassParentEnvs) {
                        GeneralCommandLine.ParentEnvironmentType.CONSOLE
                    } else {
                        GeneralCommandLine.ParentEnvironmentType.NONE
                    })
                    .withExePath(exeConfiguration.parameters.exePath)
                    .withWorkDirectory(exeConfiguration.parameters.workingDirectory)
                    .withRawParameters(exeConfiguration.parameters.programParameters)

                return runCommandLine.toProcessBuilder().start()
            }
            else {
                logger.warn("UnityExeConfiguration ${DefaultRunConfigurationGenerator.RUN_DEBUG_START_UNITY_CONFIGURATION_NAME} was not found.")
                val processBuilderArgs = getUnityArgs(project).withProjectPath(project).withRiderPath()
                return startUnity(processBuilderArgs)
            }
        }

        fun startUnity(args: MutableList<String>): Process? {
            val processBuilder = ProcessBuilder(args)
            return processBuilder.start()
        }
    }
}