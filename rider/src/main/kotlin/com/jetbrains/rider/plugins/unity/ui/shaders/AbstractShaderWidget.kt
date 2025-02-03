package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.icons.AllIcons
import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rider.editors.resolveContextWidget.ResolveContextWidgetTheme
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.ui.borders.IconBorder
import java.awt.BorderLayout
import java.awt.Component
import java.awt.event.MouseEvent
import javax.swing.JLabel
import javax.swing.JPanel

abstract class AbstractShaderWidget(val project: Project,
                                    val editor: Editor) : JPanel(BorderLayout()), RiderResolveContextWidget, Disposable {
    internal val text = Property<@NlsSafe String>("")
    protected val label = JLabel()

    init {
        text.advise(this.createLifetime()) { label.text = it }
        enableEvents(MouseEvent.MOUSE_EVENT_MASK)
        label.apply {
            foreground = null
            border = IconBorder(AllIcons.Actions.InlayDropTriangle, iconTextGap)
        }.also { add(it, BorderLayout.CENTER) }
        border = ResolveContextWidgetTheme.WIDGET_BORDER
    }

    override fun processMouseEvent(e: MouseEvent) {
        when (e.id) {
            MouseEvent.MOUSE_ENTERED -> {
                ResolveContextWidgetTheme.getHoveredAttributes().let {
                    background = it.backgroundColor
                    foreground = it.foregroundColor
                }
            }
            MouseEvent.MOUSE_EXITED -> {
                background = null
                foreground = null
            }
            MouseEvent.MOUSE_RELEASED -> {
                mousePosition?.let { showPopup(RelativePoint.getNorthWestOf(this)) }
            }
        }
    }

    override val component: Component get() = this

    override fun updateUI() {
        super.updateUI()

        // inherit background and foreground from parent
        foreground = null
        background = null
    }

    override fun dispose() {}
}