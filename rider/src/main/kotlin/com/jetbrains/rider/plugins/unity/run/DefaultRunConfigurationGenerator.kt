package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.UnityReferenceListener
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugAndPlayConfigurationType
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType

class DefaultRunConfigurationGenerator(unityReferenceDiscoverer: UnityReferenceDiscoverer, runManager: RunManager) {

    val ATTACH_CONFIGURATION_NAME = "Attach to Unity Editor"
    val ATTACH_AND_PLAY_CONFIGURATION_NAME = "Attach to Unity Editor & Play"

    init {
        unityReferenceDiscoverer.addUnityReferenceListener(object: UnityReferenceListener {
            override fun hasUnityReference() {
                // Replace the "Default" run configuration that Rider creates. If there is more than one
                // run configuration, the user has explicitly modified them. Don't create the Attach
                // config, but do make sure there's something selected.

                if (runManager.allSettings.size == 1 && runManager.allSettings[0].name == "Default") {
                    runManager.removeConfiguration(runManager.allSettings[0])
                    val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityDebugConfigurationType::class.java)
                    val runConfiguration = runManager.createRunConfiguration(ATTACH_CONFIGURATION_NAME, configurationType.attachToEditorFactory)
                    // Not shared, as that requires the entire team to have the plugin installed
                    // We'll create it if it doesn't exist, anyway
                    runManager.addConfiguration(runConfiguration, false)
                    runManager.selectedConfiguration = runConfiguration

                }
                else if (runManager.selectedConfiguration == null) {
                    val runConfiguration = runManager.findConfigurationByName(ATTACH_CONFIGURATION_NAME)
                    if (runConfiguration != null) {
                        runManager.selectedConfiguration = runConfiguration
                    }
                }
                if (!runManager.allSettings.any { a->a.type == UnityDebugAndPlayConfigurationType::class.java})
                {
                    val configurationTypeDebugAndPlay = ConfigurationTypeUtil.findConfigurationType(UnityDebugAndPlayConfigurationType::class.java)
                    val runConfigurationDebugAndPlay = runManager.createRunConfiguration(ATTACH_AND_PLAY_CONFIGURATION_NAME, configurationTypeDebugAndPlay.attachToEditorAndPlayFactory)
                    runManager.addConfiguration(runConfigurationDebugAndPlay, false)
                }
            }
        })
    }
}
