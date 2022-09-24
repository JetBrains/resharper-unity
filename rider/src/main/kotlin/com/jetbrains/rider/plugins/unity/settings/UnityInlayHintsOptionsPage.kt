package com.jetbrains.rider.plugins.unity.settings

import com.intellij.openapi.util.NlsSafe
import com.jetbrains.rider.settings.simple.SimpleOptionsPage

@Suppress("UnstableApiUsage")
class UnityInlayHintsOptionsPage : SimpleOptionsPage(pageName, "UnityInlayHintsOptions") {
    companion object {
        @NlsSafe
        const val pageName = "Unity"
    }

    override fun getId() = pageId
}
