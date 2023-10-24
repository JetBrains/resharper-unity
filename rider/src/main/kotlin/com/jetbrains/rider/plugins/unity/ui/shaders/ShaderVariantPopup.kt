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
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderKeyword
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariant

class ShaderVariantPopup(model: FrontendBackendModel) : JBPanel<ShaderVariantPopup>() {
    companion object {
        private fun createPopup(project: Project): JBPopup {
            val model = FrontendBackendHost.getInstance(project).model
            val shaderVariantPopup = ShaderVariantPopup(model)
            return JBPopupFactory.getInstance().createComponentPopupBuilder(shaderVariantPopup, shaderVariantPopup.shaderKeywords)
                .setRequestFocus(true)
                .createPopup()
        }

        fun show(project: Project, showAt: RelativePoint) {
            createPopup(project).show(showAt)
        }
    }
    
    internal val shaderKeywords = CheckBoxList<ShaderKeyword>().also { add(it) }
    private var shaderVariant: RdShaderVariant = model.defaultShaderVariant.valueOrThrow

    init {
        shaderKeywords.emptyText.text = UnityBundle.message("widgets.shaderVariants.noShaderKeywords")

        ListSpeedSearch.installOn(shaderKeywords) { it.text }

        initKeywords(model.shaderKeywords.values)
        shaderKeywords.setCheckBoxListListener(::onCheckBoxSelectionChanged)
    }

    private fun onCheckBoxSelectionChanged(index: Int, value: Boolean) {
        shaderKeywords.getItemAt(index)?.let { item ->
            shaderVariant.apply {
                val signal = if (value) enableKeyword else disableKeyword
                signal.fire(item.name)
            }
        }
    }

    private fun initKeywords(keywords: Iterable<RdShaderKeyword>) {
        val enabledKeywords = shaderVariant.enabledKeywords
        for (keyword in keywords.sortedBy { it.name }) {
            val selected = enabledKeywords.contains(keyword.name)
            addShaderKeyword(ShaderKeyword(keyword.name, selected))
        }
    }

    private fun addShaderKeyword(keyword: ShaderKeyword) {
        shaderKeywords.addItem(keyword, keyword.name, keyword.enabled)
    }

    data class ShaderKeyword(@NlsSafe val name: String, var enabled: Boolean = false)
}