package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object ShaderLabFileType : RiderLanguageFileTypeBase(ShaderLabLanguage) {
    override fun getDefaultExtension() = "shader"
    override fun getDescription() = UnityBundle.message("label.shaderlab.file")
    override fun getIcon() = UnityIcons.FileTypes.ShaderLab
    override fun getName() = "ShaderLab"
}

