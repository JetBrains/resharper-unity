package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.popup.JBPopup
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.CheckBoxList
import com.intellij.ui.ListSpeedSearch
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.components.JBPanel
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariant
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariantSet

class ShaderVariantsSelector(model: FrontendBackendModel) : JBPanel<ShaderVariantsSelector>() {
    companion object {
        private fun createPopup(project: Project): JBPopup {
            val model = FrontendBackendHost.getInstance(project).model
            val shaderVariantsSelector = ShaderVariantsSelector(model)
            return JBPopupFactory.getInstance().createComponentPopupBuilder(shaderVariantsSelector, shaderVariantsSelector.variants)
                .setRequestFocus(true)
                .createPopup()
        }

        fun show(project: Project, showAt: RelativePoint) {
            createPopup(project).show(showAt)
        }
    }
    
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
            shaderVariantSet.apply {
                val signal = if (value) selectVariant else deselectVariant
                signal.fire(item.name)
            }
        }
    }

    private fun initVariants(variants: Iterable<RdShaderVariant>) {
        val selectedVariants = shaderVariantSet.selectedVariants
        for (variant in variants.sortedBy { it.name }) {
            val selected = selectedVariants.contains(variant.name)
            addShaderVariant(ShaderVariant(variant.name, selected))
        }
    }

    private fun addShaderVariant(variant: ShaderVariant) {
        variants.addItem(variant, variant.name, variant.selected)
    }

    data class ShaderVariant(@NlsSafe val name: String, var selected: Boolean = false)
}