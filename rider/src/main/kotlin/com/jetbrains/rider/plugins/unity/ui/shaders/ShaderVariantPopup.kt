package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.popup.JBPopup
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.CheckBoxList
import com.intellij.ui.ListSpeedSearch
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.panels.ListLayout
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rd.util.reactive.valueOrThrow
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.*
import java.awt.event.ItemEvent

class ShaderVariantPopup(private val interaction: ShaderVariantInteraction, model: FrontendBackendModel) : JBPanel<ShaderVariantPopup>(ListLayout.vertical(5)) {
    companion object {
        private fun createPopup(interaction: ShaderVariantInteraction, model: FrontendBackendModel): JBPopup {
            val shaderVariantPopup = ShaderVariantPopup(interaction, model)
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
                createPopup(it.unwrap(), model).show(showAt)
            }
        }
    }

    private val shaderApiComponent = ComboBox<RdShaderApi>().also { add(it) }
    private val shaderKeywordsComponent = CheckBoxList<ShaderKeyword>().also { add(it) }
    private var shaderVariant: RdShaderVariant = model.defaultShaderVariant.valueOrThrow

    init {
        initKeywords(model.shaderKeywords.values)
        initShaderApi()
    }

    private fun onCheckBoxSelectionChanged(index: Int, value: Boolean) {
        shaderKeywordsComponent.getItemAt(index)?.let { item ->
            shaderVariant.apply {
                val signal = if (value) enableKeyword else disableKeyword
                signal.fire(item.name)
            }
        }
    }

    private fun initKeywords(keywords: Iterable<RdShaderKeyword>) {
        shaderKeywordsComponent.emptyText.text = UnityBundle.message("widgets.shaderVariants.noShaderKeywords")

        ListSpeedSearch.installOn(shaderKeywordsComponent) { it.text }

        val enabledKeywords = shaderVariant.enabledKeywords
        for (keyword in keywords.sortedWith(::compareKeywords)) {
            val selected = enabledKeywords.contains(keyword.name)
            addShaderKeyword(ShaderKeyword(keyword.name, selected))
        }
        shaderKeywordsComponent.setCheckBoxListListener(::onCheckBoxSelectionChanged)
    }

    private fun initShaderApi() {
        for (api in RdShaderApi.entries)
            shaderApiComponent.addItem(api)
        shaderApiComponent.selectedItem = shaderVariant.shaderApi.valueOrNull
        shaderApiComponent.addItemListener {
            if (it.stateChange == ItemEvent.SELECTED) {
                shaderVariant.setShaderApi.fire(it.item as RdShaderApi)
            }
        }
    }

    private fun compareKeywords(x: RdShaderKeyword, y: RdShaderKeyword): Int {
        val available = HashSet(interaction.availableKeywords)
        val xAvailable = available.contains(x.name)
        val yAvailable = available.contains(y.name)
        if (xAvailable)
            return if (yAvailable) x.name.compareTo(y.name) else -1
        if (yAvailable)
            return 1
        return x.name.compareTo(y.name)
    }

    private fun addShaderKeyword(keyword: ShaderKeyword) {
        shaderKeywordsComponent.addItem(keyword, keyword.name, keyword.enabled)
    }

    data class ShaderKeyword(@NlsSafe val name: String, var enabled: Boolean = false)
}