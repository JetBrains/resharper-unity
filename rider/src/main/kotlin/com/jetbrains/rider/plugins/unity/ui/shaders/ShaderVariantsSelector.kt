package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.CheckBoxList
import com.intellij.ui.ListSpeedSearch
import com.intellij.ui.components.JBPanel
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariant
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariantSet

class ShaderVariantsSelector(model: FrontendBackendModel) : JBPanel<ShaderVariantsSelector>() {
    internal val variants = CheckBoxList<ShaderVariant>().also { add(it) }
    private var shaderVariantSet: RdShaderVariantSet = model.defaultShaderVariantSet.valueOrThrow

    init {
        variants.emptyText.text = UnityBundle.message("widgets.shaderVariants.noShaderVariants")

        ListSpeedSearch.installOn(variants) { it.text }

        initVariants(model.shaderVariants.values)
        variants.setCheckBoxListListener(::onCheckBoxSelectionChanged)
    }

    private fun onCheckBoxSelectionChanged(index: Int, value: Boolean) {
        variants.getItemAt(index)?.let { item ->
            shaderVariantSet.selectedVariants.apply {
                if (value)
                    add(item.name)
                else
                    remove(item.name)
            }
        }
    }

    private fun initVariants(variants: Iterable<RdShaderVariant>) {
        val selectedVariants = shaderVariantSet.selectedVariants
        for (variant in variants) {
            val selected = selectedVariants.contains(variant.name)
            addShaderVariant(ShaderVariant(variant.name, selected))
        }
    }

    private fun addShaderVariant(variant: ShaderVariant) {
        variants.addItem(variant, variant.name, variant.selected)
    }

    data class ShaderVariant(@NlsSafe val name: String, var selected: Boolean = false)
}