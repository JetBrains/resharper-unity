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
import com.intellij.ui.SeparatorComponent
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.components.JBPanel
import com.intellij.ui.components.JBRadioButton
import com.intellij.ui.components.JBScrollPane
import com.intellij.ui.components.panels.ListLayout
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.common.ui.ToggleButtonModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.CreateShaderVariantInteractionArgs
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderApi
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderPlatform
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderVariantInteraction
import org.jetbrains.annotations.Nls
import java.awt.Font
import java.awt.event.ItemEvent
import java.awt.font.TextAttribute
import javax.swing.ButtonGroup
import javax.swing.JCheckBox
import javax.swing.JComponent
import javax.swing.JPanel

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

    private val shaderApiComponent = ComboBox<ShaderApiEntry>()
    private val shaderPlatformsComponent = JPanel(ListLayout.horizontal(8))
    private val shaderPlatformsGroup = ButtonGroup()
    private val shaderKeywordsComponent = MyCheckboxList()
    private val builtinDefineSymbolsComponent = CheckBoxList<String>()
    private val shaderKeywords = mutableMapOf<String, ShaderKeyword>()
    private val enabledKeywords = mutableSetOf<String>()

    init {
        border = JBUI.Borders.empty(8, 16)

        add(shaderApiComponent)
        add(shaderPlatformsComponent)
        add(JBScrollPane().apply {
            horizontalScrollBarPolicy = JBScrollPane.HORIZONTAL_SCROLLBAR_NEVER
            viewport.add(JPanel(ListLayout.vertical()).apply {
                add(builtinDefineSymbolsComponent)
                add(SeparatorComponent())
                add(shaderKeywordsComponent)
            })
        })

        initShaderApi()
        initPlatforms()
        initBuiltinDefineSymbols()
        initKeywords()
    }

    private fun onCheckBoxSelectionChanged(index: Int, value: Boolean) {
        shaderKeywordsComponent.getItemAt(index)?.let { item ->
            interaction.apply {
                val signal = if (value) enableKeyword else disableKeyword
                signal.fire(item.name)
            }
            if (value)
                enabledKeywords.add(item.name)
            else
                enabledKeywords.remove(item.name)
            updateKeywords()
        }
    }

    private fun initShaderApi() {
        for (api in ShaderApiEntry.all.values)
            shaderApiComponent.addItem(api)
        shaderApiComponent.selectedItem = ShaderApiEntry.all[interaction.shaderApi]
        shaderApiComponent.addItemListener {
            if (it.stateChange == ItemEvent.SELECTED) {
                interaction.setShaderApi.fire((it.item as ShaderApiEntry).value)
            }
            updateBuiltinDefineSymbols()
        }
    }

    private fun initPlatforms() {
        for (entry in PlatformEntry.all.values) {
            val radio = JBRadioButton(entry.name).apply {
                this.model = ToggleButtonModel(entry)
                this.model.group = shaderPlatformsGroup
                shaderPlatformsComponent.add(this)
            }
            radio.model.isSelected = entry.value == interaction.shaderPlatform
            radio.model.addItemListener {
                if (it.stateChange == ItemEvent.SELECTED) {
                    val platformEntry = (it.item as ToggleButtonModel<*>).item as PlatformEntry
                    interaction.setShaderPlatform.fire(platformEntry.value)
                }
                updateBuiltinDefineSymbols()
            }
        }
    }

    private fun initBuiltinDefineSymbols()
    {
        builtinDefineSymbolsComponent.isEnabled = false
        updateBuiltinDefineSymbols()
    }

    private fun updateBuiltinDefineSymbols()
    {
        builtinDefineSymbolsComponent.clear()

        val shaderApiDefineSymbol = (shaderApiComponent.selectedItem as ShaderApiEntry).defineSymbol
        val shaderPlatformDefineSymbol = ((shaderPlatformsGroup.selection as ToggleButtonModel<*>).item as PlatformEntry).defineSymbol
        builtinDefineSymbolsComponent.addItem(shaderApiDefineSymbol, shaderApiDefineSymbol, true)
        builtinDefineSymbolsComponent.addItem(shaderPlatformDefineSymbol, shaderPlatformDefineSymbol, true)
    }


    private fun initKeywords() {
        shaderKeywordsComponent.emptyText.text = UnityBundle.message("widgets.shaderVariants.noShaderKeywords")

        ListSpeedSearch.installOn(shaderKeywordsComponent) { it.text }

        enabledKeywords.addAll(interaction.enabledKeywords)
        for (featureKeywords in interaction.shaderFeatures) {
            for (keyword in featureKeywords) {
                if (!shaderKeywords.containsKey(keyword)) {
                    val shaderKeyword = ShaderKeyword(keyword)
                    shaderKeywords[keyword] = shaderKeyword
                    addShaderKeyword(shaderKeyword, enabledKeywords.contains(keyword))
                }
            }
        }

        updateKeywords()
        shaderKeywordsComponent.setCheckBoxListListener(::onCheckBoxSelectionChanged)
    }

    private fun updateKeywords() {
        for (keyword in shaderKeywords.values) {
            keyword.state = when {
                enabledKeywords.contains(keyword.name) -> ShaderKeywordState.SUPPRESSED
                else -> ShaderKeywordState.DISABLED
            }
        }

        for (featureKeywords in interaction.shaderFeatures) {
            for (keyword in featureKeywords) {
                if (enabledKeywords.contains(keyword)) {
                    shaderKeywords.getValue(keyword).state = ShaderKeywordState.ENABLED
                    break
                }
            }
        }
    }

    private fun addShaderKeyword(shaderKeyword: ShaderKeyword, enabled: Boolean) {
        shaderKeywordsComponent.addItem(shaderKeyword, shaderKeyword.name, enabled)
    }

    private class MyCheckboxList : CheckBoxList<ShaderKeyword>() {
        private lateinit var strikethroughFont: Font

        override fun updateUI() {
            super.updateUI()
            strikethroughFont = font.deriveFont(mapOf(TextAttribute.STRIKETHROUGH to TextAttribute.STRIKETHROUGH_ON))
        }

        override fun adjustRendering(rootComponent: JComponent,
                                     checkBox: JCheckBox,
                                     index: Int,
                                     selected: Boolean,
                                     hasFocus: Boolean): JComponent {
            if (getItemAt(index)?.state == ShaderKeywordState.SUPPRESSED)
                rootComponent.font = strikethroughFont
            return rootComponent
        }
    }

    private data class ShaderApiEntry(val value: RdShaderApi, @Nls val name: String, @NlsSafe val defineSymbol: String) {
        companion object {
            val all = sequenceOf(
                ShaderApiEntry(RdShaderApi.D3D11, UnityBundle.message("shaderVariant.popup.shaderApi.entries.d3d11"), "SHADER_API_D3D11"),
                ShaderApiEntry(RdShaderApi.Vulkan, UnityBundle.message("shaderVariant.popup.shaderApi.entries.vulkan"), "SHADER_API_VULKAN"),
                ShaderApiEntry(RdShaderApi.Metal, UnityBundle.message("shaderVariant.popup.shaderApi.entries.metal"), "SHADER_API_METAL"),
                ShaderApiEntry(RdShaderApi.GlCore, UnityBundle.message("shaderVariant.popup.shaderApi.entries.glcore"), "SHADER_API_GLCORE"),
                ShaderApiEntry(RdShaderApi.GlEs, UnityBundle.message("shaderVariant.popup.shaderApi.entries.gles"), "SHADER_API_GLES"),
                ShaderApiEntry(RdShaderApi.GlEs3, UnityBundle.message("shaderVariant.popup.shaderApi.entries.gles3"), "SHADER_API_GLES3"),
                ShaderApiEntry(RdShaderApi.D3D11L9X, UnityBundle.message("shaderVariant.popup.shaderApi.entries.d3d11l9x"), "SHADER_API_D3D11_9X"),
            ).map { it.value to it }.toMap()
        }

        override fun toString() = name
    }

    private data class PlatformEntry(val value: RdShaderPlatform, @Nls val name: String, @NlsSafe val defineSymbol: String) {
        companion object {
            val all = sequenceOf(
                PlatformEntry(RdShaderPlatform.Desktop, UnityBundle.message("shaderVariant.popup.shaderPlatform.entries.desktop"), "SHADER_API_DESKTOP"),
                PlatformEntry(RdShaderPlatform.Mobile, UnityBundle.message("shaderVariant.popup.shaderPlatform.entries.mobile"), "SHADER_API_MOBILE")
            ).map { it.value to it }.toMap()
        }

        override fun toString() = name
    }

    private enum class ShaderKeywordState {
        ENABLED,
        DISABLED,
        SUPPRESSED
    }

    private data class ShaderKeyword(@NlsSafe val name: String, var state: ShaderKeywordState = ShaderKeywordState.DISABLED)
}