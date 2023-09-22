package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.ui.components.JBPanelWithEmptyText
import java.awt.Dimension

class ShaderVariantsSelector : JBPanelWithEmptyText() {
    init {
        preferredSize = Dimension(200, 200)
        emptyText.text = "Construction site"
    }
}