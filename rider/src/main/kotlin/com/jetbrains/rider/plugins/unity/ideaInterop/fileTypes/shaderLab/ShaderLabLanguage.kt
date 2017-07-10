package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageBase

object ShaderLabLanguage : RiderLanguageBase("ShaderLab", "SHADERLAB") {
    override fun isCaseSensitive(): Boolean = false
}
