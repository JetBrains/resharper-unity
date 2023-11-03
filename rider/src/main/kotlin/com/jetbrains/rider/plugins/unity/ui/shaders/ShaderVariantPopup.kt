package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.popup.JBPopup
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.ui.CheckBoxList
import com.intellij.ui.ListSpeedSearch
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.panels.ListLayout
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.CreateShaderVariantInteractionArgs
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderApi
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderKeyword
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderVariantInteraction
import java.awt.event.ItemEvent

class ShaderVariantPopup(private val interaction: ShaderVariantInteraction) : JBPanel<ShaderVariantPopup>(ListLayout.vertical(5)) {
    companion object {
        private fun createPopup(interaction: ShaderVariantInteraction): JBPopup {
            val shaderVariantPopup = ShaderVariantPopup(interaction)
            return JBPopupFactory.getInstance().createComponentPopupBuilder(shaderVariantPopup, shaderVariantPopup.shaderKeywordsComponent)
                .setRequestFocus(true)
                .createPopup()
        }

        fun show(project: Project, editor: Editor, showAt: RelativePoint) {
            val id = editor.document.getFirstDocumentId(project) ?: return
            val model = FrontendBackendHost.getInstance(project).model
            val lifetime = LifetimeDefinition()
            EditorUtil.disposeWithEditor(editor) { lifetime.terminate() }
            model.createShaderVariantInteraction.start(lifetime, CreateShaderVariantInteractionArgs(id, editor.caretModel.offset)).result.advise(lifetime) {
                createPopup(it.unwrap()).show(showAt)
            }
        }
    }

    private val shaderApiComponent = ComboBox<RdShaderApi>().also { add(it) }
    private val shaderKeywordsComponent = CheckBoxList<RdShaderKeyword>().also { add(it) }

    init {
        initKeywords()
        initShaderApi()
    }

    private fun onCheckBoxSelectionChanged(index: Int, value: Boolean) {
        shaderKeywordsComponent.getItemAt(index)?.let { item ->
            interaction.apply {
                val signal = if (value) enableKeyword else disableKeyword
                signal.fire(item.name)
            }
        }
    }

    private fun initKeywords() {
        shaderKeywordsComponent.emptyText.text = UnityBundle.message("widgets.shaderVariants.noShaderKeywords")

        ListSpeedSearch.installOn(shaderKeywordsComponent) { it.text }

        for (keyword in interaction.shaderKeywords.sortedBy { it.name }) {
            addShaderKeyword(keyword)
        }
        shaderKeywordsComponent.setCheckBoxListListener(::onCheckBoxSelectionChanged)
    }

    private fun initShaderApi() {
        for (api in RdShaderApi.entries)
            shaderApiComponent.addItem(api)
        shaderApiComponent.selectedItem = interaction.shaderApi
        shaderApiComponent.addItemListener {
            if (it.stateChange == ItemEvent.SELECTED) {
                interaction.setShaderApi.fire(it.item as RdShaderApi)
            }
        }
    }

    private fun addShaderKeyword(keyword: RdShaderKeyword) {
        shaderKeywordsComponent.addItem(keyword, keyword.name, keyword.enabled)
    }
}