package com.jetbrains.rider.plugins.unity.run

import com.intellij.execution.RunManagerEx
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.UnityReferenceListener
import com.jetbrains.rider.plugins.unity.run.configurations.UnityDebugConfigurationType

class DefaultRunConfigurationGenerator(unityReferenceDiscoverer: UnityReferenceDiscoverer, runManagerEx: RunManagerEx) {
    init {
        unityReferenceDiscoverer.addUnityReferenceListener(object: UnityReferenceListener {
            override fun HasUnityReference() {
                if (runManagerEx.allSettings.size == 1 && runManagerEx.allSettings[0].name == "Default") {
                    runManagerEx.removeConfiguration(runManagerEx.allSettings[0])

                    val configurationType = ConfigurationTypeUtil.findConfigurationType(UnityDebugConfigurationType::class.java)
                    val runConfiguration = runManagerEx.createRunConfiguration("Attach to Unity Editor", configurationType.attachToEditorFactory)
                    // Not shared, as that requires the entire team to have the plugin installed
                    // We'll create it if it doesn't exist, anyway
                    runManagerEx.addConfiguration(runConfiguration, false)
                    runManagerEx.selectedConfiguration = runConfiguration

                    // TODO: Add another configuration "Attach to Unity Editor and Play"
                    // (runConfiguration.configuration as UnityAttachToEditorConfiguration).play = true
                }
            }
        })
    }
}
