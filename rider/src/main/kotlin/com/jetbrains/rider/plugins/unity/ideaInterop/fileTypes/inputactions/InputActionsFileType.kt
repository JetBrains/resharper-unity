package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.inputactions

import com.intellij.json.JsonFileType
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object InputActionsFileType : JsonFileType() {
    override fun getDefaultExtension() = "inputactions"
    override fun getDescription() = UnityBundle.message("label.inputactions.file.unity")
    override fun getIcon() = UnityIcons.FileTypes.InputActions
    override fun getName() = "InputActions"
    override fun getDisplayName() = "InputActions"
}
