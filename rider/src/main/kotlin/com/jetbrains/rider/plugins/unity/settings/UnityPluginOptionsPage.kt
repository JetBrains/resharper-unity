package com.jetbrains.rider.plugins.unity.settings

import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class UnityPluginOptionsPage : SimpleOptionsPage(UnityBundle.message("configurable.name.unity.engine"), "UnityPluginSettings") {
    override fun getId(): String {
        return "preferences.build.unityPlugin"
    }

    override fun getHelpTopic(): String {
        return "Settings_Unity_Engine"
    }
}
