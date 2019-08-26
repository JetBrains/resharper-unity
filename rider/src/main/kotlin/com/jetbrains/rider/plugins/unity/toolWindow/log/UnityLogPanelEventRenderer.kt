package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.RowIcon
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.speedSearch.SpeedSearchUtil
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import net.miginfocom.swing.MigLayout
import java.awt.Color
import java.awt.Component
import javax.swing.JLabel
import javax.swing.JList
import javax.swing.JPanel
import javax.swing.ListCellRenderer

class UnityLogPanelEventRenderer(logModel: UnityLogPanelModel, lifetime: Lifetime) : ColoredListCellRenderer<LogPanelItem>(), ListCellRenderer<LogPanelItem> {
    private val countLabel = JLabel()
    private val view = JPanel(MigLayout("ins 0, fillx, gap 0, novisualpadding"))

    init{
        view.add(this, "wmin 0, pushx")
        countLabel.foreground = Color.GRAY

        logModel.mergeSimilarItems.advise(lifetime) {
            if (it)
                view.add(countLabel, "east, gapbefore ${JBUI.scale(20)}, gapafter ${JBUI.scale(10)} ")
            else
                view.remove(countLabel)
        }

    }

    override fun getListCellRendererComponent(list: JList<out LogPanelItem>, item: LogPanelItem, index: Int, selected: Boolean, hasFocus: Boolean): Component {
        this.clear()
        this.font = list.getFont()
        this.mySelected = selected
        this.myForeground = if (this.isEnabled) list.getForeground() else UIUtil.getLabelDisabledForeground()
        this.mySelectionForeground = list.getSelectionForeground()
        val bg =
        if (UIUtil.isUnderWin10LookAndFeel()) {
            if (selected) list.getSelectionBackground() else list.getBackground()
        } else {
            if (selected) list.getSelectionBackground() else null
        }
        this.background = bg
        countLabel.background = bg
        view.background = bg

        this.setPaintFocusBorder(hasFocus)
        this.customizeCellRenderer(list, item, index, selected, hasFocus)

        countLabel.text = item.count.toString()
        return view
    }

    private val tokenizer: UnityLogTokenizer = UnityLogTokenizer()

    override fun customizeCellRenderer(list: JList<out LogPanelItem>, event: LogPanelItem?, index: Int, selected: Boolean, hasFocus: Boolean) {
        if (event != null) {
            icon = RowIcon(event.type.getIcon(), event.mode.getIcon())
            val tokens = tokenizer.tokenize(event.message)

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

            SpeedSearchUtil.applySpeedSearchHighlighting(list, this, true, selected)
        }
    }
}