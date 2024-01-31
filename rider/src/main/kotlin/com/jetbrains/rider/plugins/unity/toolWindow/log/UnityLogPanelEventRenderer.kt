package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ui.ColoredListCellRenderer
import com.intellij.ui.RowIcon
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.speedSearch.SpeedSearchUtil
import com.intellij.util.ui.UIUtil
import net.miginfocom.swing.MigLayout
import java.awt.Color
import java.awt.Component
import javax.swing.JLabel
import javax.swing.JList
import javax.swing.JPanel
import javax.swing.ListCellRenderer

class UnityLogPanelEventRenderer : ColoredListCellRenderer<LogPanelItem>(), ListCellRenderer<LogPanelItem> {
    private val countLabel = JLabel()
    private val view = JPanel(MigLayout("ins 0, fillx, gap 0, novisualpadding"))

    init {
        view.add(this, "wmin 0, pushx")
        countLabel.foreground = Color.GRAY

        view.add(countLabel, "east") //, gapbefore ${JBUI.scale(20)}, gapafter ${JBUI.scale(10)}
    }

    override fun getListCellRendererComponent(list: JList<out LogPanelItem>,
                                              item: LogPanelItem,
                                              index: Int,
                                              selected: Boolean,
                                              hasFocus: Boolean): Component {
        this.clear()
        this.font = list.font
        this.mySelected = selected
        this.myForeground = if (this.isEnabled) list.foreground else UIUtil.getLabelDisabledForeground()
        this.mySelectionForeground = list.selectionForeground
        val bg =
            if (UIUtil.isUnderWin10LookAndFeel()) {
                if (selected) list.selectionBackground else list.background
            }
            else {
                if (selected) list.selectionBackground else null
            }
        this.background = bg
        countLabel.background = bg
        view.background = bg

        this.setPaintFocusBorder(hasFocus)
        this.customizeCellRenderer(list, item, index, selected, hasFocus)

        if (item.count > 1)
            countLabel.text = " ×${item.count} "
        else
            countLabel.text = ""

        return view
    }

    private val tokenizer: UnityLogTokenizer = UnityLogTokenizer()

    override fun customizeCellRenderer(list: JList<out LogPanelItem>,
                                       event: LogPanelItem?,
                                       index: Int,
                                       selected: Boolean,
                                       hasFocus: Boolean) {
        if (event != null) {
            icon = RowIcon(event.type.getIcon(), event.mode.getIcon())
            val tokens = tokenizer.tokenize(event.shortPresentation)

            for (token in tokens) {
                if (!token.used) {
                    var style = SimpleTextAttributes.REGULAR_ATTRIBUTES

                    if (token.bold && token.italic) {
                        style = SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD or SimpleTextAttributes.STYLE_ITALIC, token.color)
                    }
                    else if (token.bold) {
                        style = if (token.color == null)
                            SimpleTextAttributes.REGULAR_BOLD_ATTRIBUTES
                        else
                            SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD, token.color)
                    }
                    else if (token.italic) {
                        style = if (token.color == null)
                            SimpleTextAttributes.REGULAR_ITALIC_ATTRIBUTES
                        else
                            SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD, token.color)
                    }
                    else if (token.color != null) {
                        style = SimpleTextAttributes(SimpleTextAttributes.STYLE_PLAIN, token.color)
                    }

                    append(token.token, style)
                }
            }

            SpeedSearchUtil.applySpeedSearchHighlighting(list, this, true, selected)
        }
    }
}