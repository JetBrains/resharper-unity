package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.util.IconUtil
import icons.UnityIcons

class UnityExeConfigurationType : ConfigurationTypeBase("RunUnityExe", "Standalone Player", // "RunUnityExe" preserved for compatibility
    "Unity Standalone Player configuration", IconUtil.scale(UnityIcons.RunConfigurations.UnityExe, null, 0.33f)) {

    val factory: UnityExeConfigurationFactory = UnityExeConfigurationFactory(this)

    override fun getHelpTopic(): String = "Run_Debug_Configuration_Unity_Executable"

    init {
        addFactory(factory)
    }
}
