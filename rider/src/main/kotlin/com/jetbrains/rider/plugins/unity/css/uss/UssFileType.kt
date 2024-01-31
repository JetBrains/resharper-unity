package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.openapi.fileTypes.LanguageFileType
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object UssFileType : LanguageFileType(UssLanguage) {
    override fun getIcon() = UnityIcons.FileTypes.Uss
    override fun getName() = "USS"
    override fun getDefaultExtension() = "uss"
    override fun getDescription() = UnityBundle.message("label.uielement.style.sheet.file.unity")
}