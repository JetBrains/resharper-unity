package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object CgFileType : RiderLanguageFileTypeBase(CgLanguage) {
    override fun getDefaultExtension() = "glsl"
    override fun getDescription() = UnityBundle.message("label.cg.file")
    override fun getIcon() = UnityIcons.FileTypes.Cg
    override fun getName() = "Cg"
}

