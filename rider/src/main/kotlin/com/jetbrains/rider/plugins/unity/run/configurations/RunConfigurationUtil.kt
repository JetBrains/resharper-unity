package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.*
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.RunProfile
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.impl.ExecutionManagerImpl
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ExecutionEnvironmentBuilder
import com.intellij.execution.runners.ExecutionUtil
import com.intellij.execution.ui.RunConfigurationStartHistory
import com.intellij.icons.AllIcons
import com.intellij.openapi.project.Project
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import javax.swing.Icon

/** Returns true if the "Attach to Unity Editor" run configuration exists and is running */
fun isAttachedToUnityEditor(project: Project): Boolean {
    getUnityEditorRunConfiguration(project)?.let { return isRunning(project, it) }
    return false
}

/**
 * Runs the "Attach to Unity Editor" run configuration
 *
 * If the configuration is already running, it will make sure it's the selected run configuration.
 *
 * @return `false` if the run configuration does not exist
 */
fun attachToUnityEditor(project: Project): Boolean {
    getUnityEditorRunConfiguration(project)?.let {
        startDebugRunConfiguration(project, it)
        return true
    }
    return false
}

fun attachToUnityProcess(project: Project, process: UnityProcess) {
    if (process is UnityEditor && EditorInstanceJson.getInstance(project).contents?.process_id == process.pid) {
        attachToUnityEditor(project)
        return
    }

    if (process is UnityCustomPlayer) {
        attachToCustomPlayer(project, process)
        return
    }

    val runProfile = UnityProcessRunProfile(project, process)
    val environment = ExecutionEnvironmentBuilder
        .create(project, DefaultDebugExecutor.getDebugExecutorInstance(), runProfile)
        .build()
    ProgramRunnerUtil.executeConfiguration(environment, false, true)
}

fun attachToCustomPlayer(project: Project, customPlayer: UnityCustomPlayer) {
    val runManager = RunManager.getInstance(project)
    var configurationSettings = runManager.findConfigurationByTypeAndName(
        UnityPlayerDebugConfigurationType.id,
        customPlayer.displayName
    )

    if (configurationSettings == null) {
        val configurationType =
            ConfigurationTypeUtil.findConfigurationType(UnityPlayerDebugConfigurationType::class.java)
        configurationSettings = runManager.createConfiguration(customPlayer.displayName, configurationType.attachToPlayerFactory)
        (configurationSettings.configuration as UnityPlayerDebugConfiguration).apply {
            state.playerId = customPlayer.playerId
            state.host = customPlayer.host
            state.port = customPlayer.port
            state.projectName = customPlayer.projectName
        }
        runManager.setTemporaryConfiguration(configurationSettings)
    }

    startDebugRunConfiguration(project, configurationSettings)
}

private fun getUnityEditorRunConfiguration(project: Project) =
    RunManager.getInstance(project).findConfigurationByTypeAndName(
        UnityEditorDebugConfigurationType.id,
        DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME
    )

private fun isRunning(project: Project, configurationSettings: RunnerAndConfigurationSettings) =
    ExecutionManagerImpl.getInstance(project).getRunningDescriptors { it == configurationSettings }.isNotEmpty()

private fun startDebugRunConfiguration(
    project: Project,
    configurationSettings: RunnerAndConfigurationSettings
) {
    if (isRunning(project, configurationSettings)) {
        RunManager.getInstance(project).selectedConfiguration = configurationSettings
        return
    }

    ExecutionUtil.runConfiguration(configurationSettings, DefaultDebugExecutor.getDebugExecutorInstance())
    RunManager.getInstance(project).selectedConfiguration = configurationSettings

    // The new UI only adds items to the run widget if explicitly started from the run widget or via context action
    // (IDEA-310169)
    @Suppress("UnstableApiUsage")
    RunConfigurationStartHistory.getInstance(project).register(configurationSettings)
}

/**
 * Simple [RunProfile] implementation to connect to a [UnityProcess] via the Attach To menu
 */
class UnityProcessRunProfile(private val project: Project, val process: UnityProcess)
    : RunProfile, IRiderDebuggable {

    override fun getName(): String = process.displayName
    override fun getIcon(): Icon = AllIcons.Actions.StartDebugger

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        val remoteConfiguration = object : RemoteConfiguration {
            override var address = process.host
            override var port = process.port
            override var listenPortForConnections = false
        }
        return when (process) {
            is UnityIosUsbProcess -> {
                // We need to tell the debugger which port to connect to. The proxy will open this port and forward
                // traffic to the on-device port of 56000. These port numbers are hardcoded, and follow what Unity does
                // in their debugger plugins. There is a chance that the local 12000 port is in use, but there's not
                // much we can do about that - we tell the proxy both port numbers and it tries to set things up.
                UnityAttachIosUsbProfileState(
                    project,
                    remoteConfiguration,
                    environment,
                    process.displayName,
                    process.deviceId
                )
            }

            is UnityAndroidAdbProcess -> {
                UnityAttachAndroidAdbProfileState(
                    project,
                    remoteConfiguration,
                    environment,
                    process.displayName,
                    process.deviceId
                )
            }

            is UnityLocalUwpPlayer -> {
                UnityAttachLocalUwpProfileState(
                    remoteConfiguration,
                    environment,
                    process.displayName,
                    process.packageName
                )
            }

            else -> {
                UnityAttachProfileState(
                    remoteConfiguration,
                    environment,
                    process.displayName,
                    process is UnityEditor || process is UnityEditorHelper
                )
            }
        }
    }
}
