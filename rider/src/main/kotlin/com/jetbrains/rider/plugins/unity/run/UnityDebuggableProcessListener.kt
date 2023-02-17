package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationType

class UnityDebuggableProcessListener(project: Project, lifetime: Lifetime,
                                     onProcessAdded: (UnityProcess) -> Unit,
                                     onProcessRemoved: (UnityProcess) -> Unit) {
    companion object {
        private val logger = Logger.getInstance(UnityDebuggableProcessListener::class.java)
    }

    init {
        UnityEditorListener(project, lifetime, onProcessAdded, onProcessRemoved)
        UnityPlayerListener(lifetime, onProcessAdded, onProcessRemoved)
        AppleDeviceListener(project, lifetime, onProcessAdded, onProcessRemoved)
        AndroidDeviceListener(project, lifetime, onProcessAdded, onProcessRemoved)
        enumerateCustomPlayers(project, onProcessAdded)
    }

    private fun enumerateCustomPlayers(project: Project, onProcessAdded: (UnityProcess) -> Unit) {
        logger.trace("Looking for custom players in run configurations")

        try {
            val configurationType =
                ConfigurationTypeUtil.findConfigurationType(UnityPlayerDebugConfigurationType::class.java)
            val customPlayers = RunManager.getInstance(project).getConfigurationSettingsList(configurationType)
            customPlayers.forEach {
                val configuration = it.configuration as UnityPlayerDebugConfiguration

                // The configuration's values *should* be valid, but let's have fallback, just in case
                val player = UnityCustomPlayer(
                        configuration.name,
                        configuration.state.host ?: "localhost",
                        configuration.state.port,
                        configuration.state.projectName ?: UnityBundle.message("project.name.custom")
                    )
                onProcessAdded(player)
            }
        } catch (e: Throwable) {
            logger.error(e)
        }
    }
}