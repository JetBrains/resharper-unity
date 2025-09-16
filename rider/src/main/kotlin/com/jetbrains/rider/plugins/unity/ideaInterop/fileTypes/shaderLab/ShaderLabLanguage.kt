package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.intellij.lang.Language

object ShaderLabLanguage : Language("ShaderLab") {
    override fun isCaseSensitive(): Boolean = false
}
