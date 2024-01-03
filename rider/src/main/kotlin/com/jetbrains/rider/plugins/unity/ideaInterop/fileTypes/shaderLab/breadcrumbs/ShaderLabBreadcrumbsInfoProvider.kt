package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.breadcrumbs

import com.intellij.lang.Language
import com.jetbrains.rider.breadcrumbs.BackendBreadcrumbsInfoProvider
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabLanguage

class ShaderLabBreadcrumbsInfoProvider : BackendBreadcrumbsInfoProvider() {
    override val language: Language = ShaderLabLanguage
}
