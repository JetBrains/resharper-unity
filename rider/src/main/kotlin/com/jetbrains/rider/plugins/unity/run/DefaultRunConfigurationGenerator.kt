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
                // Add Attach Unity Editor configuration, if it doesn't exist
                if (!runManager.allSettings.any { a->a.type == UnityDebugConfigurationType::class.java})
                {
                    val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityDebugConfigurationType::class.java)
                    val runConfiguration = runManager.createRunConfiguration(ATTACH_CONFIGURATION_NAME, configurationType.attachToEditorFactory)
                    // Not shared, as that requires the entire team to have the plugin installed
                    runManager.addConfiguration(runConfiguration, false)
                }
                // make Attach Unity Editor configuration selected if nothing is selected
                if (runManager.selectedConfiguration == null) {
                    val runConfiguration = runManager.findConfigurationByName(ATTACH_CONFIGURATION_NAME)
                    if (runConfiguration != null) {
                        runManager.selectedConfiguration = runConfiguration
                    }
                }

                if (!runManager.allSettings.any { a->a.type == UnityDebugAndPlayConfigurationType::class.java}) {
                    // todo:restore in 2018.2
                    // val configurationTypeDebugAndPlay = ConfigurationTypeUtil.findConfigurationType(UnityDebugAndPlayConfigurationType::class.java)
                    // val runConfigurationDebugAndPlay = runManager.createRunConfiguration(ATTACH_AND_PLAY_CONFIGURATION_NAME, configurationTypeDebugAndPlay.attachToEditorAndPlayFactory)
                    //runManager.addConfiguration(runConfigurationDebugAndPlay, false)
                }
            }
        })
    }
}
