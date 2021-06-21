package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.lang.css.CSSParserDefinition
import com.intellij.psi.FileViewProvider
import com.intellij.psi.tree.IFileElementType

class UssFileParserDefinition: CSSParserDefinition() {
    companion object {
        val USS_FILE = IFileElementType("USS_FILE", UssLanguage)
    }

    override fun createFile(viewProvider: FileViewProvider) = UssFile(viewProvider)
    override fun getFileNodeType() = USS_FILE
}