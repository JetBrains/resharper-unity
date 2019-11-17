package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss

import com.intellij.psi.FileViewProvider
import com.intellij.psi.css.impl.CssFileImpl

class UssFile(viewProvider: FileViewProvider) : CssFileImpl(viewProvider, UssLanguage) {
    override fun toString() = "UssFile:$name"
}
