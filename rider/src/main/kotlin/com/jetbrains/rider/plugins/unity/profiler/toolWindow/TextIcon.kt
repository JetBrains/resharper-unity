package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.ide.ui.UISettings
import com.intellij.util.ui.JBUI
import java.awt.Component
import java.awt.Font
import java.awt.FontMetrics
import java.awt.Graphics
import java.awt.Graphics2D
import java.awt.Color
import javax.swing.Icon

/**
 * An icon that combines a base icon with text rendered next to it.
 */
internal class TextIcon(
    private val baseIcon: Icon?,
    private val text: String,
    private val font: Font,
    private val fontMetrics: FontMetrics,
    private val foreground: Color
) : Icon {
    private val gap = JBUI.scale(2)

    override fun paintIcon(c: Component, g: Graphics, x: Int, y: Int) {
        val g2 = g.create() as Graphics2D
        try {
            UISettings.setupAntialiasing(g2)
            var currentX = x

            if (baseIcon != null) {
                val iconY = y + (iconHeight - baseIcon.iconHeight) / 2
                baseIcon.paintIcon(c, g2, currentX, iconY)
                currentX += baseIcon.iconWidth + gap
            }

            g2.font = font
            g2.color = foreground
            val textY = y + (iconHeight + fontMetrics.ascent - fontMetrics.descent) / 2
            g2.drawString(text, currentX, textY)
        } finally {
            g2.dispose()
        }
    }

    override fun getIconWidth(): Int {
        val baseWidth = baseIcon?.iconWidth ?: 0
        val textWidth = fontMetrics.stringWidth(text)
        return baseWidth + (if (baseWidth > 0 && text.isNotEmpty()) gap else 0) + textWidth
    }

    override fun getIconHeight(): Int {
        val baseHeight = baseIcon?.iconHeight ?: 0
        return maxOf(baseHeight, fontMetrics.height)
    }
}
