package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.CheckBoxList
import com.intellij.ui.ListSpeedSearch
import com.intellij.ui.components.JBPanel
import com.jetbrains.rider.plugins.unity.UnityBundle

class ShaderVariantsSelector : JBPanel<ShaderVariantsSelector>() {
    internal val variants = CheckBoxList<ShaderVariant>().also { add(it) }

    init {
        variants.emptyText.text = UnityBundle.message("widgets.shaderVariants.noShaderVariants")

        addShaderVariant(ShaderVariant("VARIANT 1"))
        addShaderVariant(ShaderVariant("VARIANT 2"))
        addShaderVariant(ShaderVariant("VARIANT 3"))

        ListSpeedSearch.installOn(variants) { it.text }
    }

    private fun addShaderVariant(variant: ShaderVariant) {
        variants.addItem(variant, variant.name, variant.selected)
    }

    data class ShaderVariant(@NlsSafe val name: String, var selected: Boolean = false)
}