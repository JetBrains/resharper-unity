package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmref

import com.intellij.json.JsonFileType
import icons.UnityIcons

object AsmRefFileType : JsonFileType() {
    override fun getDefaultExtension() = "asmref"
    override fun getDescription() = "Assembly definition reference file (Unity)"
    override fun getIcon() = UnityIcons.FileTypes.AsmRef
    override fun getName() = "AsmRef"
    override fun getDisplayName() = "AsmRef"
}
