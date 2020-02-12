package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.UnknownConfigurationType
import com.intellij.openapi.project.Project
import com.jetbrains.rdclient.util.idea.LifetimedProjectComponent
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorAndPlayFactory
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToEditorFactory
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.UnityHost

class DefaultRunConfigurationGenerator(project: Project, runManager: RunManager) : LifetimedProjectComponent(project) {

    companion object {
        const val ATTACH_CONFIGURATION_NAME = "Attach to Unity Editor"
        const val ATTACH_AND_PLAY_CONFIGURATION_NAME = "Attach to Unity Editor & Play"
    }

    init {
        UnityHost.getInstance(project).model.hasUnityReference.whenTrue(componentLifetime) {
            // Clean up the renamed "attach and play" configuration from 2018.2 EAP1-3
            // (Was changed from a separate configuration type to just another factory under "Attach to Unity")
            val toRemove = runManager.allSettings.filter {
                it.type is UnknownConfigurationType && it.name == ATTACH_AND_PLAY_CONFIGURATION_NAME
            }
            for (value in toRemove) {
                runManager.removeConfiguration(value)
            }

            // Add "Attach Unity Editor" configurations, if they don't exist
            if (!runManager.allSettings.any { it.type is UnityDebugConfigurationType && it.factory is UnityAttachToEditorFactory }) {
                val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityDebugConfigurationType::class.java)
                val runConfiguration = runManager.createConfiguration(ATTACH_CONFIGURATION_NAME, configurationType.attachToEditorFactory)
                // Not shared, as that requires the entire team to have the plugin installed
                runManager.addConfiguration(runConfiguration, false)
            }

            if (!runManager.allSettings.any { it.type is UnityDebugConfigurationType && it.factory is UnityAttachToEditorAndPlayFactory }) {
                val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityDebugConfigurationType::class.java)
                val runConfiguration = runManager.createConfiguration(ATTACH_AND_PLAY_CONFIGURATION_NAME, configurationType.attachToEditorAndPlayFactory)
                runManager.addConfiguration(runConfiguration, false)
            }

            // make Attach Unity Editor configuration selected if nothing is selected
            if (runManager.selectedConfiguration == null) {
                val runConfiguration = runManager.findConfigurationByName(ATTACH_CONFIGURATION_NAME)
                if (runConfiguration != null) {
                    runManager.selectedConfiguration = runConfiguration
                }
            }
        }
    }
}