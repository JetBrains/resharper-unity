package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml

import com.intellij.psi.FileViewProvider
import com.intellij.psi.impl.source.xml.XmlFileImpl

class UxmlFile(viewProvider: FileViewProvider?) : XmlFileImpl(viewProvider, UxmlFileParserDefinition.UXML_FILE) {
    override fun toString() = "UxmlFile:$name"
}
