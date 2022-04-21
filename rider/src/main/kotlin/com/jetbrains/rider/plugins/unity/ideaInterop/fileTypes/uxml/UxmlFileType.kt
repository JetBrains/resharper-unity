package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml

import com.intellij.ide.highlighter.XmlLikeFileType
import UnityIcons

object UxmlFileType : XmlLikeFileType(UxmlLanguage) {
    override fun getIcon() = UnityIcons.FileTypes.Uxml
    override fun getName() = "UXML"
    override fun getDefaultExtension() = "uxml"
    override fun getDescription() = "UIElement UXML File (Unity)"
}