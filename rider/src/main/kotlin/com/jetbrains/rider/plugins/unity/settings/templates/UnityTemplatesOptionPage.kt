package com.jetbrains.rider.plugins.unity.settings.templates

import com.intellij.openapi.options.Configurable
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class UnityFileTemplatesOptionPage: SimpleOptionsPage("Unity", "RiderUnityFileTemplatesSettings"), Configurable.NoScroll {
    override fun getId(): String {
        return "RiderUnityFileTemplatesSettings"
    }
}

class UnityLiveTemplatesOptionPage: SimpleOptionsPage("Unity", "RiderUnityLiveTemplatesSettings"), Configurable.NoScroll {
    override fun getId(): String {
        return "RiderUnityLiveTemplatesSettings"
    }
}