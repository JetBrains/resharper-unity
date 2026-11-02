package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.icons.AllIcons
import com.intellij.ide.actions.ShowLogAction
import com.intellij.ui.dsl.builder.AlignY
import com.intellij.ui.dsl.builder.Cell
import com.intellij.ui.dsl.builder.DslComponentProperty
import com.intellij.ui.dsl.builder.IntelliJSpacingConfiguration
import com.intellij.ui.dsl.builder.Panel
import com.intellij.ui.dsl.builder.RightGap
import com.intellij.ui.dsl.builder.VerticalComponentGap
import com.intellij.ui.dsl.builder.components.DslLabel
import com.intellij.ui.dsl.builder.impl.DslComponentPropertyInternal
import com.intellij.ui.scale.JBUIScale
import org.jetbrains.annotations.Nls
import java.awt.Dimension
import java.awt.Point
import java.awt.Rectangle
import javax.swing.JComponent
import javax.swing.JEditorPane
import javax.swing.JPanel
import kotlin.math.max
import kotlin.math.min

/**
 * Draws the Try Again button and the error icon beside the failure message, and the Show Log link below them.
 *
 * The button and the icon stay centered on the message when the message wraps.
 */
internal fun Panel.failureRows(@Nls tryAgain: String, @Nls message: String, retry: () -> Unit) {
    row {
        panel {
            row {
                button(tryAgain) { retry() }.gap(RightGap.SMALL)
                icon(AllIcons.General.Error)
            }.resizableRow()
        }.align(AlignY.FILL).gap(RightGap.SMALL)
        comment(message).align(AlignY.CENTER).wrapBesideControl()
    }

    if (ShowLogAction.isSupported()) {
        row {
            link(ShowLogAction.getActionName()) { ShowLogAction.showLog() }
        }
    }
}

/**
 * Lets a word-wrapped text beside a control narrow to the free width of the page.
 *
 * A Settings page is as wide as its widest row asks for. With the default width, a text beside a control
 * can make the page wider than the window.
 */
internal fun Cell<JEditorPane>.wrapBesideControl(): Cell<JEditorPane> = applyToComponent {
    putClientProperty(DslComponentPropertyInternal.PREFERRED_COLUMNS_LABEL_WORD_WRAP, BESIDE_CONTROL_COLUMNS)
}

/**
 * Shows [link] directly after the word-wrapped [text], on the first line of the text.
 *
 * The text keeps its natural width while the row has room for it, and wraps in the free width otherwise.
 * Add the component with `AlignX.FILL` as the last cell of a row.
 */
internal class LinkAfterText(private val text: DslLabel, private val link: JComponent?) : JPanel(null) {
    init {
        isOpaque = false
        putClientProperty(DslComponentProperty.VERTICAL_COMPONENT_GAP, VerticalComponentGap.BOTH)
        add(text)
        if (link != null) add(link)
    }

    override fun getPreferredSize(): Dimension {
        val narrowText = text.getFontMetrics(text.font).charWidth('0') * BESIDE_CONTROL_COLUMNS
        return Dimension(min(text.preferredSize.width, narrowText) + linkSpace(), arrange(width).height)
    }

    override fun getMinimumSize(): Dimension = preferredSize

    override fun doLayout() {
        val arrangement = arrange(width)
        text.bounds = arrangement.text
        if (link != null && arrangement.link != null) link.bounds = arrangement.link
    }

    override fun getBaseline(width: Int, height: Int): Int = arrange(width).baseline

    override fun getBaselineResizeBehavior(): BaselineResizeBehavior = BaselineResizeBehavior.CONSTANT_ASCENT

    private fun gap(): Int = JBUIScale.scale(IntelliJSpacingConfiguration().horizontalDefaultGap)

    private fun linkSpace(): Int = if (link == null) 0 else gap() + link.preferredSize.width

    private fun arrange(width: Int): Arrangement {
        val natural = text.preferredSize.width
        val textWidth = if (width > 0) min(natural, width - linkSpace()).coerceAtLeast(0) else natural
        val textHeight = text.preferredSize.height
        val textBaseline = text.getBaseline(textWidth, textHeight)
        val linkSize = link?.preferredSize
        val linkBaseline = if (link == null || linkSize == null) 0 else link.getBaseline(linkSize.width, linkSize.height).coerceAtLeast(0)
        val baseline = max(textBaseline, linkBaseline)
        val textBounds = Rectangle(0, baseline - textBaseline, textWidth, textHeight)
        val linkBounds = linkSize?.let { Rectangle(Point(textWidth + gap(), baseline - linkBaseline), it) }
        return Arrangement(textBounds, linkBounds, baseline)
    }

    private class Arrangement(val text: Rectangle, val link: Rectangle?, val baseline: Int) {
        val height: Int get() = max(text.y + text.height, link?.let { it.y + it.height } ?: 0)
    }
}

private const val BESIDE_CONTROL_COLUMNS = 20
