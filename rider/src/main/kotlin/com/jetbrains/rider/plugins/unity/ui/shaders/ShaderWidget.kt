package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.icons.AllIcons
import com.intellij.openapi.Disposable
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.model.frontendBackend.AutoShaderContextData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.SelectShaderContextDataInteraction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextDataBase
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import icons.UnityIcons
import org.jetbrains.annotations.Nls
import java.awt.BorderLayout
import java.awt.Component
import java.awt.event.MouseAdapter
import java.awt.event.MouseEvent
import javax.swing.JLabel
import javax.swing.JPanel
import javax.swing.SwingConstants


class ShaderWidget(val project: Project, val editor: Editor) : JPanel(BorderLayout()), RiderResolveContextWidget, Disposable {
    companion object {
        @Nls
        private fun getContextPresentation(data : ShaderContextData) = "${data.name}:${data.startLine}"
    }

    private val label = JLabel(UnityIcons.FileTypes.ShaderLab)
    private val widgetLifetime = this.createLifetime()
    private val currentContextData : IProperty<ShaderContextData?> = Property(null)

    init {
        label.apply {
            icon = AllIcons.Actions.InlayDropTriangle
            text = "..."
            foreground = null
            horizontalTextPosition = SwingConstants.LEFT

            addMouseListener(object : MouseAdapter() {
                override fun mouseReleased(e: MouseEvent?) {
                    showPopup(this@apply)
                }
            })
        }.also { add(it) }

        isVisible = false

        currentContextData.advise(project.lifetime) {
            if (it == null) {
                label.text = UnityUIBundle.message("auto")
                label.toolTipText = UnityUIBundle.message("default.file.and.symbol.context")
            } else {
                label.text = getContextPresentation(it)
                label.toolTipText = UnityUIBundle.message("file.and.symbol.context.derived.from.include.at.context", getContextPresentation(it))
            }
        }
    }

    override val component: Component = this
    override fun update() = Unit

    override fun updateUI() {
        super.updateUI()

        // inherit background and foreground from parent
        foreground = null
        background = null
    }

    fun setData(data: ShaderContextDataBase?) {
        when (data) {
            is AutoShaderContextData -> currentContextData.value = null
            is ShaderContextData -> currentContextData.value = data
        }
        isVisible = data != null
    }

    fun showPopup(label: JLabel) {
        val lt = widgetLifetime.createNested()
        val id = editor.document.getFirstDocumentId(project) ?: return
        val host = FrontendBackendHost.getInstance(project)
        host.model.createSelectShaderContextInteraction.start(lt, id).result.advise(lt) {
            try {
                val interaction = it.unwrap()

                val actions = createActions(interaction)
                val group = DefaultActionGroup().apply {
                    addAll(actions)
                }
                val popup = ShaderContextPopup(group, SimpleDataContext.getProjectContext(project), currentContextData)
                val terminateLifetime = Runnable { lt.terminate(true) }
                popup.setFinalRunnable(terminateLifetime)
                // in current implementation it isn't possible to combine multiple setFinalRunnable and if action performed then it will override final runnable and lifetime won't be terminated
                // to work around this we add onPerformed callback for every possible action
                for (action in actions)
                    action.onPerformed = terminateLifetime
                popup.show(RelativePoint(label, label.mousePosition))
            } catch (t: Throwable) {
                lt.terminate(true)
                throw t
            }
        }
    }

    private fun createActions(interaction: SelectShaderContextDataInteraction): List<AbstractShaderContextSwitchAction> {
        val result = mutableListOf<AbstractShaderContextSwitchAction>(ShaderAutoContextSwitchAction(interaction, currentContextData))
        for (index in 0 until interaction.items.size) {
            result.add(ShaderContextSwitchAction(interaction, index, currentContextData))
        }
        return result
    }

    override fun dispose() {}
}