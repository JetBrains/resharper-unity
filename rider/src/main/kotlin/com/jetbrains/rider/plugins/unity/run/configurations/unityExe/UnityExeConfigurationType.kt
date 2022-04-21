package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.configurations.ConfigurationTypeBase
import UnityIcons

class UnityExeConfigurationType : ConfigurationTypeBase(id, "Standalone Player", // "RunUnityExe" preserved for compatibility
                                                        "Unity Standalone Player configuration", UnityIcons.RunConfigurations.UnityExe) {

    val factory: UnityExeConfigurationFactory = UnityExeConfigurationFactory(this)

    override fun getHelpTopic(): String = "Run_Debug_Configuration_Unity_Executable"

    init {
        addFactory(factory)
    }

    companion object {
        const val id = "RunUnityExe"
    }
}
