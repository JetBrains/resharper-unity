package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss

import com.intellij.psi.css.impl.util.editor.CssBreadcrumbsInfoProvider

// Allows enabling/disabling breadcrumbs for USS
class UssFileBreadcrumbsProvider: CssBreadcrumbsInfoProvider() {
    override fun getLanguages()= arrayOf(UssLanguage)
}