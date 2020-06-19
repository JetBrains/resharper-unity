package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.util.IconUtil
import icons.RiderIcons

// TODO(enkn): use Unity icon from RiderIcons.RunConfiguration
class UnityExeConfigurationType : ConfigurationTypeBase("RunUnityExe", "Unity Executable",
    "Unity executable configuration", IconUtil.scale(RiderIcons.Wizard.Unity, null, 0.33f)) {

    val factory: UnityExeConfigurationFactory = UnityExeConfigurationFactory(this)

    override fun getHelpTopic(): String = "Run_Debug_Configuration_Unity_Executable"

    init {
        addFactory(factory)
    }

}
