package com.jetbrains.rider.plugins.unity.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

@Suppress("UnstableApiUsage")
class UnityInlayHintsOptionsPage : SimpleOptionsPage("Unity", "UnityInlayHintsOptions") {
    override fun getId() = pageId
}
