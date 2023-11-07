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
import com.intellij.ui.components.RadioButton
import com.intellij.ui.components.panels.ListLayout
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.CreateShaderVariantInteractionArgs
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderApi
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderPlatform
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderVariantInteraction
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

    private val shaderApiComponent = ComboBox<ShaderApiEntry>().also { add(it) }
    private val shaderPlatformsGroup = JPanel(ListLayout.horizontal(8)).also { add(it) }
    private val shaderKeywordsComponent = MyCheckboxList().also { add(it) }
    private val shaderKeywords = mutableMapOf<String, ShaderKeyword>()
    private val enabledKeywords = mutableSetOf<String>()

    init {
        border = JBUI.Borders.empty(8, 16)

        initShaderApi()
        initPlatforms()
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
        }
    }

    private fun initPlatforms() {
        val platformsGroup = ButtonGroup()
        for (entry in PlatformEntry.all.values) {
            val radio = RadioButton(entry.name).apply {
                this.model.group = platformsGroup
                shaderPlatformsGroup.add(this)
            }
            radio.model.isSelected = entry.value == interaction.shaderPlatform
            radio.model.addItemListener {
                if (it.stateChange == ItemEvent.SELECTED) {
                    interaction.setShaderPlatform.fire(entry.value)
                }
            }
        }
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

    private data class ShaderApiEntry(val value: RdShaderApi, val name: String, val defineSymbol: String) {
        companion object {
            val all = mapOf(
                Pair(RdShaderApi.D3D11, ShaderApiEntry(RdShaderApi.D3D11, "DirectX 11", "SHADER_API_D3D11")),
                Pair(RdShaderApi.Vulkan, ShaderApiEntry(RdShaderApi.Vulkan, "Vulkan", "SHADER_API_VULKAN")),
                Pair(RdShaderApi.Metal, ShaderApiEntry(RdShaderApi.Metal, "Metal (iOS, Mac)", "SHADER_API_METAL")),
                Pair(RdShaderApi.GlCore, ShaderApiEntry(RdShaderApi.GlCore, "OpenGL Core (3/4)", "SHADER_API_GLCORE")),
                Pair(RdShaderApi.GlEs, ShaderApiEntry(RdShaderApi.GlEs, "Open GL ES 2.0", "SHADER_API_GLES")),
                Pair(RdShaderApi.GlEs3, ShaderApiEntry(RdShaderApi.GlEs3, "Open GL ES 3.0/3.1", "SHADER_API_GLES3")),
                Pair(RdShaderApi.D3D11L9X, ShaderApiEntry(RdShaderApi.D3D11L9X, "DirectX 11 (feature level 9.x for UWP)", "SHADER_API_D3D11_9X")),
            )
        }

        override fun toString() = name
    }

    private data class PlatformEntry(val value: RdShaderPlatform, val name: String, val defineSymbol: String) {
        companion object {
            val all = mapOf(
                Pair(RdShaderPlatform.Desktop, PlatformEntry(RdShaderPlatform.Desktop, "Desktop", "SHADER_API_DESKTOP")),
                Pair(RdShaderPlatform.Mobile, PlatformEntry(RdShaderPlatform.Mobile, "Mobile", "SHADER_API_MOBILE"))
            )
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