package com.jetbrains.resharper.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.jetbrains.resharper.ideaInterop.fileTypes.RiderLanguageBase

object ShaderLabLanguage : RiderLanguageBase("ShaderLab", "SHADERLAB") {
    override fun isCaseSensitive(): Boolean = false
}
