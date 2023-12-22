package com.jetbrains.rider.plugins.unity.settings

import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class UnityInlayHintsOptionsPage : SimpleOptionsPage(pageName, "UnityInlayHintsOptions") {
    companion object {
        @NlsSafe
        const val pageName = "Unity"
    }

    override fun getId() = pageId

    override fun getHelpTopic(): String {
        return "Settings_Inlay_Hints_Unity"
    }
}
