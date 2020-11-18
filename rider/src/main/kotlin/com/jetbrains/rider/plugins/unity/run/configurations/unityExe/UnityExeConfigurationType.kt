package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.configurations.ConfigurationTypeBase
import icons.UnityIcons

class UnityExeConfigurationType : ConfigurationTypeBase("RunUnityExe", "Unity Executable", // "RunUnityExe" preserved for compatibility
    "Either UnityEditor or Unity Standalone Player", UnityIcons.RunConfigurations.UnityExe) {

    val factory: UnityExeConfigurationFactory = UnityExeConfigurationFactory(this)

    override fun getHelpTopic(): String = "Run_Debug_Configuration_Unity_Executable"

    init {
        addFactory(factory)
    }
}
