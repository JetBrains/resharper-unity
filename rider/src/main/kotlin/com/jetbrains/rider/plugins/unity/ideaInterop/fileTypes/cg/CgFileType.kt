package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import icons.UnityIcons

object CgFileType : RiderLanguageFileTypeBase(CgLanguage) {
    override fun getDefaultExtension() = "glsl"
    override fun getDescription() = "Cg file"
    override fun getIcon() = UnityIcons.FileTypes.Cg
    override fun getName() = "Cg"
}

