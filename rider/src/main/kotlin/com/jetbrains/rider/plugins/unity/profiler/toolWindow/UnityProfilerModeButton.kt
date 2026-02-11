package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.icons.AllIcons
import com.intellij.ide.ui.UISettings
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerChartViewModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.ui.components.SplitButton
import java.awt.*
import javax.swing.Icon

internal class UnityProfilerModeButton private constructor(
    private val viewModel: UnityProfilerChartViewModel,
    lifetime: Lifetime,
    private val modeActions: DefaultActionGroup
) : SplitButton(
    createMainModeAction(viewModel),
    createInitialPresentation(viewModel),
    modeActions,
    "UnityProfilerChart",
    ActionToolbar.DEFAULT_MINIMUM_BUTTON_SIZE
) {

    constructor(viewModel: UnityProfilerChartViewModel, lifetime: Lifetime) : this(viewModel, lifetime, DefaultActionGroup())

    private val autoModeAction = object : DumbAwareAction(UnityUIBundle.message("unity.profiler.integration.auto")) {
        override fun actionPerformed(e: AnActionEvent) {
            viewModel.fetchingMode.set(FetchingMode.Auto)
        }
    }

    private val manualModeAction = object : DumbAwareAction(UnityUIBundle.message("unity.profiler.integration.manual")) {
        override fun actionPerformed(e: AnActionEvent) {
            viewModel.fetchingMode.set(FetchingMode.Manual)
        }
    }

    init {
        viewModel.fetchingMode.advise(viewModel.lifetime.intersect(lifetime)) { mode ->
            presentation.text = if (mode == FetchingMode.Auto) UnityUIBundle.message("unity.profiler.integration.auto") else UnityUIBundle.message("unity.profiler.integration.manual")
            presentation.icon = if (mode == FetchingMode.Auto) AllIcons.General.InspectionsOK else null

            modeActions.removeAll()
            if (mode == FetchingMode.Auto) {
                modeActions.add(manualModeAction)
            } else {
                modeActions.add(autoModeAction)
            }

            revalidate()
            repaint()
        }
    }

    override fun isOutlined(): Boolean = true

    override fun getPreferredSize(): Dimension {
        val size = super.getPreferredSize()
        val text = presentation.getText(true)
        if (!text.isNullOrEmpty()) {
            val executeIconWidth = (myAction.templatePresentation.icon ?: AllIcons.Toolbar.Unknown).iconWidth
            val iconWidth = getIcon()?.iconWidth ?: 0
            size.width = size.width - executeIconWidth + iconWidth
        }
        return size
    }

    override fun getIcon(): Icon? {
        val baseIcon = super.getIcon()
        val text = presentation.getText(true)
        if (text.isNullOrEmpty()) return baseIcon
        return ModeIcon(baseIcon, text, font, getFontMetrics(font), if (isEnabled) foreground else UIUtil.getInactiveTextColor())
    }

    private class ModeIcon(
        val baseIcon: Icon?,
        val text: String,
        val font: Font,
        val fm: FontMetrics,
        val foreground: Color
    ) : Icon {
        private val gap = JBUI.scale(2)

        override fun paintIcon(c: Component, g: Graphics, x: Int, y: Int) {
            val g2 = g.create() as Graphics2D
            try {
                UISettings.setupAntialiasing(g2)
                var currentX = x
                if (baseIcon != null) {
                    val iconY = y + (getIconHeight() - baseIcon.getIconHeight()) / 2
                    baseIcon.paintIcon(c, g2, currentX, iconY)
                    currentX += baseIcon.getIconWidth() + gap
                }

                g2.font = font
                g2.color = foreground
                val textY = y + (getIconHeight() + fm.ascent - fm.descent) / 2
                g2.drawString(text, currentX, textY)
            }
            finally {
                g2.dispose()
            }
        }

        override fun getIconWidth(): Int {
            val baseWidth = baseIcon?.getIconWidth() ?: 0
            val textWidth = fm.stringWidth(text)
            return baseWidth + (if (baseWidth > 0 && text.isNotEmpty()) gap else 0) + textWidth
        }

        override fun getIconHeight(): Int {
            val baseHeight = baseIcon?.getIconHeight() ?: 0
            return maxOf(baseHeight, fm.height)
        }
    }

    companion object {
        private fun createMainModeAction(viewModel: UnityProfilerChartViewModel) = object : DumbAwareAction() {
            override fun actionPerformed(e: AnActionEvent) {
                // Action will be added later
            }

            override fun update(e: AnActionEvent) {
                val mode = viewModel.fetchingMode.valueOrNull ?: FetchingMode.Auto
                e.presentation.text = if (mode == FetchingMode.Auto) UnityUIBundle.message("unity.profiler.integration.auto") else UnityUIBundle.message("unity.profiler.integration.manual")
                e.presentation.icon = if (mode == FetchingMode.Auto) AllIcons.General.InspectionsOK else null
            }

            override fun getActionUpdateThread() = ActionUpdateThread.EDT
        }

        private fun createInitialPresentation(viewModel: UnityProfilerChartViewModel) = Presentation().apply {
            val mode = viewModel.fetchingMode.valueOrNull ?: FetchingMode.Auto
            text = if (mode == FetchingMode.Auto) UnityUIBundle.message("unity.profiler.integration.auto") else UnityUIBundle.message("unity.profiler.integration.manual")
            icon = if (mode == FetchingMode.Auto) AllIcons.General.InspectionsOK else null
        }
    }
}
