package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.psi.FileViewProvider
import com.intellij.psi.css.impl.CssFileImpl

internal class UssFile(viewProvider: FileViewProvider) : CssFileImpl(viewProvider, UssLanguage) {
    override fun toString() = "UssFile:$name"
}
