package com.jetbrains.rider.plugins.unity.ui.borders

import com.intellij.ui.scale.JBUIScale
import java.awt.Component
import java.awt.Graphics
import java.awt.Insets
import javax.swing.Icon
import javax.swing.border.Border

class IconBorder(val icon: Icon, private val hgap: Int = 0) : Border {
    override fun paintBorder(c: Component, g: Graphics, x: Int, y: Int, width: Int, height: Int) {
        icon.paintIcon(c, g, x + width - icon.iconWidth, (height - icon.iconHeight) / 2)
    }

    @Suppress("UseDPIAwareInsets")
    override fun getBorderInsets(c: Component): Insets = Insets(0, 0, 0, icon.iconWidth + JBUIScale.scale(hgap))

    override fun isBorderOpaque(): Boolean = false
}