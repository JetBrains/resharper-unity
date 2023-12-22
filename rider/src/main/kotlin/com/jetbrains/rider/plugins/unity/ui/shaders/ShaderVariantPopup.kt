package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.icons.AllIcons
import com.intellij.ide.BrowserUtil
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.ComboBox
import com.intellij.openapi.ui.JBPopupMenu
import com.intellij.openapi.ui.popup.JBPopup
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.*
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.components.*
import com.intellij.ui.components.labels.LinkLabel
import com.intellij.ui.components.panels.HorizontalLayout
import com.intellij.ui.components.panels.ListLayout
import com.intellij.ui.components.panels.VerticalLayout
import com.intellij.util.ui.JBDimension
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.util.lifetime.LifetimeDefinition
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.common.ui.ToggleButtonModel
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.CreateShaderVariantInteractionArgs
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderApi
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderPlatform
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderVariantInteraction
import org.jetbrains.annotations.Nls
import java.awt.Color
import java.awt.Dimension
import java.awt.Font
import java.awt.event.ItemEvent
import java.awt.font.TextAttribute
import javax.swing.*

class ShaderVariantPopup(private val project: Project, private val interaction: ShaderVariantInteraction, @Nls val contextName: String) : JBPanel<ShaderVariantPopup>(VerticalLayout(5)) {
    companion object {

        val ENABLED_SEPARATOR_FOREGROUND: Color = JBColor.namedColor("Group.separatorColor", JBColor(Gray.xCD, Gray.x51))
        private fun createPopup(project: Project, interaction: ShaderVariantInteraction, @Nls contextName: String): JBPopup {
            val shaderVariantPopup = ShaderVariantPopup(project, interaction, contextName)
            return JBPopupFactory.getInstance().createComponentPopupBuilder(shaderVariantPopup, shaderVariantPopup.shaderKeywordsComponent)
                .setRequestFocus(true)
                .createPopup()
        }

        fun show(project: Project, editor: Editor, showAt: RelativePoint) {
            val id = editor.document.getFirstDocumentId(project) ?: return
            val model = FrontendBackendHost.getInstance(project).model
            val lifetime = LifetimeDefinition()
            EditorUtil.disposeWithEditor(editor) { lifetime.terminate() }

            val activity = ShaderVariantEventLogger.logShowShaderVariantPopupStarted(project)
            model.createShaderVariantInteraction.start(lifetime, CreateShaderVariantInteractionArgs(id, editor.caretModel.offset)).result.advise(lifetime) { result ->
                try {
                    val contextName = editor.virtualFile?.takeIf { it.fileType !is ShaderLabFileType }?.name ?: UnityBundle.message(
                        "shaderVariant.popup.shaderProgram")
                    val interaction = result.unwrap()
                    createPopup(project, interaction, contextName).show(showAt)

                    activity?.finished {
                        listOf(ShaderVariantEventLogger.DEFINE_COUNT with interaction.availableKeywords)
                    }
                } catch (e: Throwable) {
                    activity?.finished {
                        listOf(ShaderVariantEventLogger.DEFINE_COUNT with -1)
                    }

                    throw e
                }
            }

        }
    }

    private val shaderApiComponent = ComboBox<ShaderApiEntry>()
    private val shaderPlatformsComponent = JPanel(ListLayout.horizontal(8))
    private val shaderPlatformsGroup = ButtonGroup()
    private val shaderKeywordsComponent = MyCheckboxList()
    private val builtinDefineSymbolsComponent = CheckBoxList<String>()
    private val otherEnabledKeywordsLabel = JBLabel()

    private val shaderKeywords = mutableMapOf<String, ShaderKeyword>()
    private val enabledKeywords = mutableSetOf<String>()
    private var ownEnabledKeywordsCount = 0
    private var noNotifyKeywordChange = false

    init {
        initShaderApi()
        initPlatforms()
        initBuiltinDefineSymbols()
        initKeywords()

        border = JBUI.Borders.empty(8, 16)

        val boldFont = RelativeFont.BOLD.derive(font)

        add(shaderApiComponent)
        add(shaderPlatformsComponent)
        val mainBackground = this.background
        add(JBScrollPane().apply {
            horizontalScrollBarPolicy = JBScrollPane.HORIZONTAL_SCROLLBAR_NEVER
            maximumSize = JBDimension.size(Dimension(Int.MAX_VALUE, 500))
            border = JBUI.Borders.empty()

            viewport.add(JPanel(VerticalLayout(0)).apply {
                border = JBUI.Borders.empty()

                builtinDefineSymbolsComponent.background = mainBackground
                add(builtinDefineSymbolsComponent)
                add(SeparatorComponent(ENABLED_SEPARATOR_FOREGROUND, SeparatorOrientation.HORIZONTAL).apply {
                    setVGap(JBUI.CurrentTheme.List.buttonSeparatorInset())
                })
                
                add(JBLabel(UnityBundle.message("shaderVariant.popup.keywords.label", contextName)).apply {
                    isEnabled = false
                    font = boldFont
                })

                shaderKeywordsComponent.background = mainBackground
                add(shaderKeywordsComponent)
            })
        })
        add(otherEnabledKeywordsLabel.apply {
            isEnabled = false
        })
        
        val horizontalContainer = JBPanel<JBPanel<*>>(HorizontalLayout(5))

        horizontalContainer.add(LinkLabel<ActionGroup>(UnityBundle.message("shaderVariant.popup.reset.link.text"), AllIcons.Actions.InlayDropTriangle).apply {
            horizontalTextPosition = SwingConstants.LEFT
            setListener({ linkLabel, group ->
                            val popup = ActionManager.getInstance().createActionPopupMenu("ShaderVariantWidget", group)
                            JBPopupMenu.showBelow(linkLabel, popup.component)
                        }, ResetActionGroup(this@ShaderVariantPopup))
        }, HorizontalLayout.LEFT)

        horizontalContainer.add(ActionLink(UnityBundle.message("shaderVariant.popup.learnMore")) {
            BrowserUtil.open("https://jb.gg/wacf2b")
            ShaderVariantEventLogger.logLearnMore(project)
        }, HorizontalLayout.RIGHT)
        
        add(horizontalContainer)
    }

    fun getOwnKeywordsCount() = ownEnabledKeywordsCount

    private fun onCheckBoxSelectionChanged(index: Int, value: Boolean) {
        if (noNotifyKeywordChange)
            return

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

        ShaderVariantEventLogger.logDefineChanged(project)
    }

    private fun disableKeywordsForCurrentContext() {
        val checkboxList = shaderKeywordsComponent
        withNoNotifyKeywordChange {
            val disabledKeywords = mutableListOf<String>()
            for (shaderKeyword in shaderKeywords.values) {
                if (shaderKeyword.state != ShaderKeywordState.DISABLED) {
                    checkboxList.setItemSelected(shaderKeyword, false)
                    enabledKeywords.remove(shaderKeyword.name)
                    disabledKeywords.add(shaderKeyword.name)
                }
            }
            interaction.disableKeywords.fire(disabledKeywords)
            updateKeywords()
        }
    }

    private fun disableKeywordsInAllContexts() {
        val checkboxList = shaderKeywordsComponent
        withNoNotifyKeywordChange {
            for (shaderKeyword in shaderKeywords.values) {
                if (shaderKeyword.state != ShaderKeywordState.DISABLED) {
                    checkboxList.setItemSelected(shaderKeyword, false)
                }
            }
            interaction.disableKeywords.fire(enabledKeywords.toList())
            enabledKeywords.clear()
            updateKeywords()
        }
    }

    private inline fun <reified T> withNoNotifyKeywordChange(action: () -> T) {
        noNotifyKeywordChange = true
        try {
            action()
        }
        finally {
            noNotifyKeywordChange = false
        }
    }

    private fun initShaderApi() {
        for (api in ShaderApiEntry.all.values) {
            shaderApiComponent.addItem(api)
        }

        shaderApiComponent.selectedItem = ShaderApiEntry.all[interaction.shaderApi]
        shaderApiComponent.addItemListener {
            if (it.stateChange == ItemEvent.SELECTED) {
                interaction.setShaderApi.fire((it.item as ShaderApiEntry).value)
            }
            updateBuiltinDefineSymbols()

            if (it.stateChange == ItemEvent.SELECTED) {
                ShaderVariantEventLogger.logApiChanged(project, (it.item as ShaderApiEntry).value)
            }
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

                if (it.stateChange == ItemEvent.SELECTED) {
                    val platformEntry = (it.item as ToggleButtonModel<*>).item as PlatformEntry
                    ShaderVariantEventLogger.logPlatformChanged(project, platformEntry.value)
                }
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
                }
            }
        }

        updateKeywords()
        for (shaderKeyword in shaderKeywords.values.sortedBy { it.name })
            addShaderKeyword(shaderKeyword, shaderKeyword.state != ShaderKeywordState.DISABLED)

        shaderKeywordsComponent.setCheckBoxListListener(::onCheckBoxSelectionChanged)
    }

    private fun updateKeywords() {
        ownEnabledKeywordsCount = 0
        for (keyword in shaderKeywords.values) {
            if (enabledKeywords.contains(keyword.name)) {
                ++ownEnabledKeywordsCount
                keyword.state = ShaderKeywordState.SUPPRESSED
            } else {
                keyword.state = ShaderKeywordState.DISABLED
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

        val otherKeywordsCount = enabledKeywords.size - ownEnabledKeywordsCount
        otherEnabledKeywordsLabel.text = UnityBundle.message("shaderVariant.popup.active.in.other.contexts", otherKeywordsCount)
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

    @Suppress("DialogTitleCapitalization")
    private class ResetCurrentContextKeywords(private val shaderVariantPopup: ShaderVariantPopup) : DumbAwareAction() {
        override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

        override fun update(e: AnActionEvent) {
            e.presentation.isEnabled = shaderVariantPopup.ownEnabledKeywordsCount > 0
            e.presentation.text = UnityBundle.message("shaderVariant.popup.reset.for.context.text", shaderVariantPopup.contextName, shaderVariantPopup.ownEnabledKeywordsCount)
        }

        override fun actionPerformed(e: AnActionEvent) {
            shaderVariantPopup.disableKeywordsForCurrentContext()

            e.project?.let {
                ShaderVariantEventLogger.logResetKeywords(it)
            }
        }
    }

    @Suppress("DialogTitleCapitalization")
    private class ResetAllContextsKeywords(private val shaderVariantPopup: ShaderVariantPopup) : DumbAwareAction() {
        override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

        override fun update(e: AnActionEvent) {
            e.presentation.isEnabled = shaderVariantPopup.enabledKeywords.size > 0
            e.presentation.text = UnityBundle.message("shaderVariant.popup.reset.in.all.contexts.text", shaderVariantPopup.enabledKeywords.size)
        }

        override fun actionPerformed(e: AnActionEvent) {
            shaderVariantPopup.disableKeywordsInAllContexts()

            e.project?.let {
                ShaderVariantEventLogger.logResetAllKeywords(it)
            }
        }
    }

    private class ResetActionGroup(shaderVariantPopup: ShaderVariantPopup) : ActionGroup() {
        private val actions = arrayOf<AnAction>(ResetCurrentContextKeywords(shaderVariantPopup), ResetAllContextsKeywords(shaderVariantPopup))

        override fun getChildren(e: AnActionEvent?): Array<AnAction> = actions
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

    private class ShaderKeyword(@NlsSafe val name: String, var state: ShaderKeywordState = ShaderKeywordState.DISABLED)
}