package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml

import com.intellij.xml.breadcrumbs.XmlLanguageBreadcrumbsInfoProvider

// Allows enabling/disabling breadcrumbs for UXML
internal class UxmlFileBreadcrumbsProvider : XmlLanguageBreadcrumbsInfoProvider() {
    override fun getLanguages() = arrayOf(UxmlLanguage)
}