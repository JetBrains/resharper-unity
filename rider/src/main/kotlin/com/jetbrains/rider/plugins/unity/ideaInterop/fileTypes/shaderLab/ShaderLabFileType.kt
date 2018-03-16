package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.unity.util.UnityIcons

object ShaderLabFileType : RiderLanguageFileTypeBase(ShaderLabLanguage) {
    override fun getDefaultExtension() = "shader"
    override fun getDescription() = "ShaderLab file"
    override fun getIcon() = UnityIcons.Icons.ShaderLabFile
    override fun getName() = "ShaderLab"
}

