package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml

import com.intellij.ide.highlighter.XmlLikeFileType
import com.intellij.openapi.util.NlsContexts.Label
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons

object UxmlFileType : XmlLikeFileType(UxmlLanguage) {
    override fun getIcon() = UnityIcons.FileTypes.Uxml
    override fun getName() = "UXML"
    override fun getDefaultExtension() = "uxml"
    override fun getDescription() = UnityBundle.message("label.uielement.uxml.file.unity")
}