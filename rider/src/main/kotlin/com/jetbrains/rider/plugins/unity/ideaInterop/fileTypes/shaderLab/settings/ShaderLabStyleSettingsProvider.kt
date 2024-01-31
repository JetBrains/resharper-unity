package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.settings

import com.intellij.lang.Language
import com.intellij.openapi.util.NlsContexts
import com.intellij.psi.codeStyle.CodeStyleConfigurable
import com.intellij.psi.codeStyle.CodeStyleSettings
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabLanguage
import com.jetbrains.rider.settings.IRiderViewModelConfigurable
import com.jetbrains.rider.settings.RiderLanguageCodeStyleSettingsProvider
import com.jetbrains.rider.settings.RiderSettingsBundle

class ShaderLabStyleSettingsProvider : RiderLanguageCodeStyleSettingsProvider() {
    override fun getConfigurableDisplayName(): String {
        return language.displayName
    }

    override fun createConfigurable(baseSettings: CodeStyleSettings, modelSettings: CodeStyleSettings): CodeStyleConfigurable {
        return createRiderConfigurable(baseSettings, modelSettings, language, configurableDisplayName)
    }

    override fun getLanguage(): Language = ShaderLabLanguage

    override fun getPagesId(): Map<String, @NlsContexts.TabTitle String> {
        return mapOf(
            "ShaderLabFormattingStylePage" to RiderSettingsBundle.message("configurable.codestyle.title.formattingStyle"))
    }

    override fun getHelpTopic(): String = "Settings_Code_Style_SHADERLAB"

    override fun filterPages(filterTag: String): Map<String, String> {
        if (filterTag == IRiderViewModelConfigurable.EditorConfigFilterTag)
            return mapOf(
                "ShaderLabFormattingStylePage" to RiderSettingsBundle.message("configurable.codestyle.title.formattingStyle"))

        return super.filterPages(filterTag)
    }
}