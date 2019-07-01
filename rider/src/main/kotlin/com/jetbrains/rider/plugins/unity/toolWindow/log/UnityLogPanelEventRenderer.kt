package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.RowIcon
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.speedSearch.SpeedSearchUtil
import java.awt.Color
import javax.swing.JList

class UnityLogPanelEventRenderer : ColoredListCellRenderer<LogPanelItem>() {

    private val tokenizer: UnityLogTokenizer = UnityLogTokenizer()

    override fun customizeCellRenderer(list: JList<out LogPanelItem>, event: LogPanelItem?, index: Int, selected: Boolean, hasFocus: Boolean) {
        if (event != null) {
            icon = RowIcon(event.type.getIcon(), event.mode.getIcon())
            var countPresentation = ""
            if (event.count > 1)
                countPresentation = " (" + event.count + ")"

            var tokens = tokenizer.tokenize(event.message)

            for (token in tokens) {
                if (!token.used) {
                    var style = SimpleTextAttributes.REGULAR_ATTRIBUTES

                    if (token.bold && token.italic)
                        style = SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD or SimpleTextAttributes.STYLE_ITALIC, token.color)
                    else if (token.bold)
                        if (token.color == null)
                            style = SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES
                        else
                            style = SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD, token.color)
                    else if (token.italic)
                        if (token.color == null)
                            style = SimpleTextAttributes.REGULAR_ITALIC_ATTRIBUTES
                        else
                            style = SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD, token.color)
                    else if(token.color != null)
                        style = SimpleTextAttributes(SimpleTextAttributes.STYLE_PLAIN, token.color)

                    append(token.token, style)
                }
            }

            append(countPresentation)
            SpeedSearchUtil.applySpeedSearchHighlighting(list, this, true, selected)
        }
    }


}