package com.jetbrains.rider.plugins.unity.settings

import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class UnityProfilerOptionsPage : SimpleOptionsPage(UnityBundle.message("configurable.name.unity.profiler"), "UnityProfilerOptionsPage") {
    override fun getId(): String {
        return "preferences.build.unityPlugin.profiler"
    }

    override fun getHelpTopic(): String {
        return "Settings_Unity_Engine_Profiler_Integration"
    }
}