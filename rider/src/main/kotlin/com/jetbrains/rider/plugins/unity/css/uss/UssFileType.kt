package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.openapi.fileTypes.LanguageFileType
import UnityIcons

object UssFileType: LanguageFileType(UssLanguage) {
    override fun getIcon() = UnityIcons.FileTypes.Uss
    override fun getName() = "USS"
    override fun getDefaultExtension() = "uss"
    override fun getDescription() = "UIElement Style Sheet File (Unity)"
}