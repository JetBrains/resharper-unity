package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmref

import com.intellij.json.JsonFileType
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object AsmRefFileType : JsonFileType() {
    override fun getDefaultExtension() = "asmref"
    override fun getDescription() = UnityBundle.message("label.assembly.definition.reference.file.unity")
    override fun getIcon() = UnityIcons.FileTypes.AsmRef
    override fun getName() = "AsmRef"
    override fun getDisplayName() = "AsmRef"
}
