package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import com.jetbrains.rider.plugins.unity.util.UnityIcons

object AsmDefFileType : RiderLanguageFileTypeBase(AsmDefLanguage) {
    override fun getDefaultExtension() = "asmdef"
    override fun getDescription() = "Assembly Definition File (Unity)"
    override fun getIcon() = UnityIcons.FileTypes.AsmDef
    override fun getName() = "AsmDef"
}
