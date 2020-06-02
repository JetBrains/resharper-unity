package com.jetbrains.rider.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class UnityPluginOptionsPage : SimpleOptionsPage("Unity Engine", "UnityPluginSettings") {
    override fun getId(): String {
        return "preferences.build.unityPlugin"
    }
}
