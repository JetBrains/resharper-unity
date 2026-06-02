package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManager
import com.intellij.execution.RunManagerListener
import com.intellij.execution.RunnerAndConfigurationSettings
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfiguration
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationType

class UnityRunManagerListener {

    fun startListening(project: Project,
                       lifetime: Lifetime,
                       onProcessAdded: (UnityDebugTarget) -> Unit,
                       onProcessRemoved: (UnityDebugTarget) -> Unit) {
        enumerateCustomPlayers(project, onProcessAdded)

        project.messageBus.connect(lifetime.createNestedDisposable())
            .subscribe(RunManagerListener.TOPIC, object : RunManagerListener {
                override fun runConfigurationAdded(settings: RunnerAndConfigurationSettings) {
                    super.runConfigurationAdded(settings)
                    if (settings.type is UnityPlayerDebugConfigurationType) {
                        processPlayer(settings, onProcessAdded)
                    }
                }

                override fun runConfigurationRemoved(settings: RunnerAndConfigurationSettings) {
                    super.runConfigurationRemoved(settings)
                    if (settings.type is UnityPlayerDebugConfigurationType) {
                        processPlayer(settings, onProcessRemoved)
                    }
                }
        })
    }

    private fun enumerateCustomPlayers(project: Project, onProcessAdded: (UnityDebugTarget) -> Unit) {
        thisLogger().trace("Looking for custom players in run configurations")

        try {
            val configurationType =
                ConfigurationTypeUtil.findConfigurationType(UnityPlayerDebugConfigurationType::class.java)
            val customPlayers = RunManager.getInstance(project).getConfigurationSettingsList(configurationType)
            customPlayers.forEach {
                processPlayer(it, onProcessAdded)
            }
        }
        catch (e: Throwable) {
            thisLogger().error(e)
        }
    }

    private fun processPlayer(it: RunnerAndConfigurationSettings, onProcessAdded: (UnityDebugTarget) -> Unit) {
        val configuration = it.configuration as UnityPlayerDebugConfiguration

        if (UnityCustomPlayer.isCustomPlayer(configuration.state.playerId)) {
            // The configuration's values *should* be valid, but let's have fallback, just in case
            val player = UnityCustomPlayer(
                configuration.name,
                configuration.state.host ?: "localhost",
                configuration.state.port,
                configuration.state.projectName ?: UnityBundle.message("project.name.custom")
            )
            onProcessAdded(player)
        }
    }
}
