package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.unity.util.UnityIcons

object CgFileType : RiderLanguageFileTypeBase(CgLanguage) {
    override fun getDefaultExtension() = "cginc"
    override fun getDescription() = "Cg file"
    override fun getIcon() = UnityIcons.Icons.ShaderLabFile
    override fun getName() = "Cg"
}

