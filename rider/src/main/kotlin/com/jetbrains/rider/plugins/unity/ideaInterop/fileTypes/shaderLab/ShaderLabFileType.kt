package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import icons.UnityIcons

object ShaderLabFileType : RiderLanguageFileTypeBase(ShaderLabLanguage) {
    override fun getDefaultExtension() = "shader"
    override fun getDescription() = "ShaderLab file"
    override fun getIcon() = UnityIcons.FileTypes.ShaderLab
    override fun getName() = "ShaderLab"
}

