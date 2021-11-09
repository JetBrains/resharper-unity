package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.json.JsonFileType
import icons.UnityIcons

object AsmDefFileType : JsonFileType() {
    override fun getDefaultExtension() = "asmdef"
    override fun getDescription() = "Assembly definition file (Unity)"
    override fun getIcon() = UnityIcons.FileTypes.AsmDef
    override fun getName() = "AsmDef"
    override fun getDisplayName() = "AsmDef"
}
