package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.actionSystem.impl.ActionButton
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.markup.InspectionWidgetActionProvider
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.ui.GotItTooltip
import com.intellij.ui.awt.RelativePoint
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.ide.editor.getExtensionSafe
import com.jetbrains.rdclient.document.textControlModel
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.WidgetAction
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariantExtension
import java.awt.*
import java.net.URL
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.plaf.basic.BasicHTML
import javax.swing.text.View

class ShaderVariantWidgetActionProvider : InspectionWidgetActionProvider {
    override fun createAction(editor: Editor): AnAction? {
        editor.project ?: return null
        return ActionManager.getInstance().getAction("ShaderVariantWidgetAction")
    }
}

class ShaderVariantWidgetAction : WidgetAction("ShaderVariantsService"){
    override fun updateInternal(e: AnActionEvent, widget: RiderResolveContextWidget) {
        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        val shaderVariantWidget = widget as? ShaderVariantWidget ?: return
        if (editor.isViewer) {
            e.presentation.isEnabledAndVisible = false
            return
        }

        e.presentation.text = shaderVariantWidget.text.value
    }

    override fun createCustomComponent(presentation: Presentation, place: String): JComponent {
        val component = object : ActionButton(this, presentation, place, JBUI.size(18)) {
            override fun getInsets(): Insets = JBUI.insets(2)
            override fun paintComponent(g: Graphics?) {
                val view = BasicHTML.createHTMLView(JLabel(), presentation.text)
                // Enable anti-aliasing for smoother text
                (g as Graphics2D).setRenderingHint(
                    RenderingHints.KEY_TEXT_ANTIALIASING,
                    RenderingHints.VALUE_TEXT_ANTIALIAS_ON
                )

                // Hover
                val look = buttonLook
                look.paintBackground(g, this)
                look.paintBorder(g, this)

                // Centering inside the Rect
                val viewWidth = view.getPreferredSpan(View.X_AXIS).toInt()
                val viewHeight = view.getPreferredSpan(View.Y_AXIS).toInt()
                val x = (size.width - viewWidth) / 2
                val y = (size.height - viewHeight) / 2

                // Paint the HTML view within the component's bounding box
                view.paint(g, Rectangle(x, y, viewWidth, viewHeight))
            }

            override fun getPreferredSize(): Dimension? {
                val view = BasicHTML.createHTMLView(JLabel(), presentation.text)
                val width = view.getPreferredSpan(View.X_AXIS).toInt()
                val height = view.getPreferredSpan(View.Y_AXIS).toInt()
                return Dimension(width, height)
            }
        }

        return component
    }
}

class ShaderVariantWidget(project: Project, editor: Editor, shaderVariant: RdShaderVariantExtension) : AbstractShaderWidget(project,
                                                                                                                            editor), RiderResolveContextWidget, Disposable {
    private val lifetime = createLifetime()

    init {
        label.icon = ShaderLabFileType.icon

        shaderVariant.info.advise(lifetime) { info ->
            val newText = when {
                info.suppressedCount > 0 -> UnityBundle.message("shaderVariant.widget.text.with.suppressed", info.enabledCount,
                                                                info.suppressedCount, info.availableCount)
                else -> UnityBundle.message("shaderVariant.widget.text", info.enabledCount, info.availableCount)
            }
            text.set(newText)
        }
    }

    override fun showPopup(point: RelativePoint) = ShaderVariantPopup.show(lifetime, project, editor, point)

    override fun addNotify() {
        editor.textControlModel?.getExtensionSafe<RdShaderVariantExtension>()
        //model.getExtension<ShaderVar>()
        super.addNotify()
        GotItTooltip("shader_variant.widget.got.it", UnityBundle.message("shaderVariant.widget.got.it.tooltip.text"), this)
            .withHeader(UnityBundle.message("shaderVariant.widget.got.it.tooltip.header"))
            .withBrowserLink(UnityBundle.message("shaderVariant.widget.got.it.tooltip.learnMore"), URL("https://jb.gg/wacf2b"))
            .show(this, GotItTooltip.TOP_MIDDLE)
    }
}