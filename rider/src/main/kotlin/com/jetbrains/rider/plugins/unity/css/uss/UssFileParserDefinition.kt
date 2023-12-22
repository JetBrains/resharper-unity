package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.lang.css.CSSParserDefinition
import com.intellij.psi.FileViewProvider

class UssFileParserDefinition: CSSParserDefinition() {
    override fun createFile(viewProvider: FileViewProvider) = UssFile(viewProvider)
    override fun getFileNodeType() = UssFileElementType.USS_FILE
}
