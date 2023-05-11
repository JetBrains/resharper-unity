package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.RunManager
import com.intellij.execution.RunnerAndConfigurationSettings
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.impl.ExecutionManagerImpl
import com.intellij.execution.runners.ExecutionUtil
import com.intellij.execution.ui.RunConfigurationStartHistory
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.run.*
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson

/**
 * Returns true if any "Attach to Unity Editor" or "Attach to Unity Editor & Play" run configuration is running
 */
fun isAttachedToUnityEditor(project: Project): Boolean {
    // The "Attach to Unity Editor" run configurations are not user configurable - if any are running, we're attached to
    // the current editor. We only support attaching to the current editor via these run configurations.
    val configurationType =
        ConfigurationTypeUtil.findConfigurationType(UnityEditorDebugConfigurationType::class.java)
    return RunManager.getInstance(project).getConfigurationSettingsList(configurationType).any {
        isRunning(project, it)
    }
}

/**
 * Runs the "Attach to Unity Editor" run configuration
 *
 * If the configuration is already running, it will make sure it's the selected run configuration. If the run
 * configuration doesn't exist, it will be recreated
 */
fun attachToUnityEditor(project: Project) {
    val configurationSettings = getUnityEditorRunConfiguration(project)
        ?: createAttachToUnityEditorConfiguration(
            project,
            DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME,
            false
        )

    startDebugRunConfiguration(project, configurationSettings)
}

/**
 * Attach the debugger to the given Unity process, reusing or creating an appropriate run configuration
 */
fun attachToUnityProcess(project: Project, process: UnityProcess) {
    if (process is UnityEditor && EditorInstanceJson.getInstance(project).contents?.process_id == process.pid) {
        attachToUnityEditor(project)
        return
    }

    // Try to find an existing run configuration for the project on the chosen player/process. Note that we might have
    // a run config for the same player with a different project
    val runManager = RunManager.getInstance(project)
    var configurationSettings = runManager.allSettings.firstOrNull {
        (it.configuration as? UnityPlayerDebugConfiguration)?.state?.playerId == process.id
            && (it.configuration as? UnityPlayerDebugConfiguration)?.state?.projectName == process.projectName
    }

    if (configurationSettings == null) {
        // Make sure "Unity" and "AssetImportWorker0" have a bit more context in the run config
        val displayName = when (process) {
            is UnityEditor -> process.displayName + " (${process.projectName ?: "Unknown Project"})"
            is UnityEditorHelper -> process.displayName + " (${process.projectName ?: "Unknown Project"})"
            else -> process.displayName
        }

        val configurationType =
            ConfigurationTypeUtil.findConfigurationType(UnityPlayerDebugConfigurationType::class.java)
        configurationSettings = runManager.createConfiguration(displayName, configurationType.attachToPlayerFactory)
        (configurationSettings.configuration as UnityPlayerDebugConfiguration).apply {
            state.playerId = process.id
            state.host = process.host
            state.port = process.port
            state.projectName = process.projectName

            when (process) {
                is UnityIosUsbProcess -> {
                    state.deviceId = process.deviceId
                    state.deviceName = process.deviceDisplayName
                }
                is UnityAndroidAdbProcess -> {
                    state.deviceId = process.deviceId
                    state.deviceName = process.deviceDisplayName
                    state.androidPackageUid = process.packageUid
                    state.packageName = process.packageName
                }
                is UnityLocalUwpPlayer -> state.packageName = process.packageName
                is UnityEditor -> state.pid = process.pid
                is UnityEditorHelper -> {
                    state.pid = process.pid
                    state.roleName = process.roleName
                }
                else -> {}
            }
        }
        runManager.setTemporaryConfiguration(configurationSettings)
    }

    startDebugRunConfiguration(project, configurationSettings)
}

fun createAttachToUnityEditorConfiguration(project: Project, name: String, play: Boolean): RunnerAndConfigurationSettings {
    val runManager = RunManager.getInstance(project)
    val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityEditorDebugConfigurationType::class.java)
    val factory = if (play) configurationType.attachToEditorAndPlayFactory else configurationType.attachToEditorFactory
    val runConfiguration = runManager.createConfiguration(name, factory)
    // No need to share it - we recreate it if it's missing
    runConfiguration.storeInLocalWorkspace()
    runManager.addConfiguration(runConfiguration)
    return runConfiguration
}

fun removeRunConfigurations(project: Project, predicate: (RunnerAndConfigurationSettings) -> Boolean) {
    val runManager = RunManager.getInstance(project)
    runManager.allSettings.filter { predicate(it) }.forEach { runManager.removeConfiguration(it) }
}

private fun getUnityEditorRunConfiguration(project: Project): RunnerAndConfigurationSettings? {
    val runManager = RunManager.getInstance(project)

    // Find the "Attach to Unity Editor" run configuration. If we can't find it, look for a renamed configuration,
    // without the "& Play" setting
    return runManager.findConfigurationByTypeAndName(
        UnityEditorDebugConfigurationType.id, DefaultRunConfigurationGenerator.ATTACH_CONFIGURATION_NAME
    ) ?: ConfigurationTypeUtil.findConfigurationType(UnityEditorDebugConfigurationType::class.java).let { type ->
        runManager.getConfigurationSettingsList(type).firstOrNull {
            !(it.configuration as UnityAttachToEditorRunConfiguration).play
        }
    }
}

private fun isRunning(project: Project, configurationSettings: RunnerAndConfigurationSettings) =
    ExecutionManagerImpl.getInstance(project).getRunningDescriptors { it == configurationSettings }.isNotEmpty()

private fun startDebugRunConfiguration(
    project: Project,
    configurationSettings: RunnerAndConfigurationSettings
) {
    if (!isRunning(project, configurationSettings)) {
        ExecutionUtil.runConfiguration(configurationSettings, DefaultDebugExecutor.getDebugExecutorInstance())
    }
    RunManager.getInstance(project).selectedConfiguration = configurationSettings

    // The new UI only adds items to the run widget if explicitly started from the run widget or via context action
    // (IDEA-310169)
    RunConfigurationStartHistory.getInstance(project).register(configurationSettings)
}
