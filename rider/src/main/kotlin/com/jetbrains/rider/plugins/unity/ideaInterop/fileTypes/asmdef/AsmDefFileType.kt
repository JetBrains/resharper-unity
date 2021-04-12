package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.json.JsonFileType
import com.intellij.json.JsonLanguage
import com.intellij.openapi.fileTypes.LanguageFileType
import com.jetbrains.rider.ideaInterop.fileTypes.RiderLanguageFileTypeBase
import icons.UnityIcons

object AsmDefFileType : JsonFileType() {
    override fun getDefaultExtension() = "asmdef"
    override fun getDescription() = "Assembly Definition File (Unity)"
    override fun getIcon() = UnityIcons.FileTypes.AsmDef
    override fun getName() = "AsmDef"
    override fun getDisplayName() = "AsmDef"
}
