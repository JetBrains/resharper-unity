package com.jetbrains.rider.plugins.unity.settings.fileLayout

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class AdditionalFileLayoutOptionsPage : SimpleOptionsPage("Additional File Layout", "UnityAdditionalFileLayout") {
    override fun getId(): String {
        return "preferences.build.unityFileLayout"
    }
}