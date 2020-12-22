package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml

import com.intellij.lang.xml.XMLParserDefinition
import com.intellij.psi.FileViewProvider
import com.intellij.psi.tree.IFileElementType

class UxmlFileParserDefinition: XMLParserDefinition() {
    companion object {
        val UXML_FILE = IFileElementType("UXML_FILE", UxmlLanguage)
    }

    override fun createFile(viewProvider: FileViewProvider) = UxmlFile(viewProvider)
    override fun getFileNodeType() = UXML_FILE
}