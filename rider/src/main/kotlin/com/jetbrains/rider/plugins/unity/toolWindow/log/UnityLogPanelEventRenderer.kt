package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.RowIcon
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.speedSearch.SpeedSearchUtil
import com.jetbrains.rider.plugins.unity.editorPlugin.model.*
import javax.swing.JList

class UnityLogPanelEventRenderer : ColoredListCellRenderer<RdLogEvent>() {
    override fun customizeCellRenderer(list: JList<out RdLogEvent>, event: RdLogEvent?, index: Int, selected: Boolean, hasFocus: Boolean) {
        if (event != null) {
            icon = RowIcon(event.type.getIcon(), event.mode.getIcon())
            append(event.message, SimpleTextAttributes.REGULAR_ATTRIBUTES)
            SpeedSearchUtil.applySpeedSearchHighlighting(list, this, true, selected)
        }
    }
}