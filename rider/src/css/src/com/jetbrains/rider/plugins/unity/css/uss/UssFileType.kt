package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.openapi.fileTypes.LanguageFileType
import com.jetbrains.rider.plugins.unity.css.uss.impl.UnityCssBundle
import icons.UnityIcons

object UssFileType : LanguageFileType(UssLanguage) {
    override fun getIcon() = UnityIcons.FileTypes.Uss
    override fun getName() = "USS"
    override fun getDefaultExtension() = "uss"
    override fun getDescription() = UnityCssBundle.message("label.uielement.style.sheet.file.unity")
}