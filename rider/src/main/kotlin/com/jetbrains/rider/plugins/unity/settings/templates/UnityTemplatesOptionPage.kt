package com.jetbrains.rider.plugins.unity.settings.templates

import com.intellij.openapi.options.Configurable
import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class UnityFileTemplatesOptionPage : SimpleOptionsPage(pageName, "RiderUnityFileTemplatesSettings"), Configurable.NoScroll {
    companion object {
        @NlsSafe
        const val pageName = "Unity"
    }

    override fun getId(): String {
        return "RiderUnityFileTemplatesSettings"
    }

    override fun getHelpTopic(): String {
        return "Reference__Templates_Explorer__File_Templates_Unity"
    }
}

class UnityLiveTemplatesOptionPage : SimpleOptionsPage(pageName, "RiderUnityLiveTemplatesSettings"), Configurable.NoScroll {
    companion object {
        @NlsSafe
        const val pageName = "Unity"
    }

    override fun getId(): String {
        return "RiderUnityLiveTemplatesSettings"
    }

    override fun getHelpTopic(): String {
        return "Reference__Templates_Explorer__Live_Templates_Unity"
    }
}