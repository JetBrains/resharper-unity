package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

class UnityExeConfigurationType : ConfigurationTypeBase(
    id,
    UnityBundle.message("configuration.type.name.standalone.player"),
    UnityBundle.message("configuration.type.description.unity.standalone.player.configuration"),
    UnityIcons.RunConfigurations.UnityExe
) {
    val factory: UnityExeConfigurationFactory = UnityExeConfigurationFactory(this)

    override fun getHelpTopic(): String = "Run_Debug_Configuration_Unity_Executable"

    init {
        addFactory(factory)
    }

    companion object {
        // "RunUnityExe" preserved for compatibility
        const val id = "RunUnityExe"
    }
}
