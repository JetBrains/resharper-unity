package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.json.JsonFileType
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object AsmDefFileType : JsonFileType() {
    override fun getDefaultExtension() = "asmdef"
    override fun getDescription() = UnityBundle.message("label.assembly.definition.file.unity")
    override fun getIcon() = UnityIcons.FileTypes.AsmDef
    override fun getName() = "AsmDef"
    override fun getDisplayName() = "AsmDef"
}
