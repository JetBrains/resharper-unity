package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.glsl

import com.jetbrains.rider.cpp.fileType.CppFileType
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.ReSharperIcons

object GlslFileType : CppFileType() {
    override fun getDefaultExtension() = "glsl"
    override fun getDescription() = UnityBundle.message("filetype.GlslFileType.description")
    override fun getIcon() = ReSharperIcons.PsiSymbols.ShaderGlsl
    override fun getName() = "GLSL"
    override fun getDisplayName() = UnityBundle.message("filetype.GlslFileType.name")
    override fun isSecondary(): Boolean = true
}
