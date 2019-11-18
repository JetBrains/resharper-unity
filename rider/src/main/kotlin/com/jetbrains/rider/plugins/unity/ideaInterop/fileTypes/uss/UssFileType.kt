package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss

import com.intellij.openapi.fileTypes.LanguageFileType
import icons.UnityIcons

object UssFileType: LanguageFileType(UssLanguage) {
    override fun getIcon() = UnityIcons.FileTypes.Asset
    override fun getName() = "USS"
    override fun getDefaultExtension() = "uss"
    override fun getDescription() = "UIElement Style Sheet File (Unity)"
}