package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionGroup
import com.intellij.openapi.actionSystem.ActionToolbar
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.Presentation
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerSnapshotModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.ui.components.SplitButton
import java.awt.Dimension
import javax.swing.Icon

internal class UnityProfilerModeButton(
    private val snapshotModel: UnityProfilerSnapshotModel,
    lifetime: Lifetime
) : SplitButton(
    createMainModeAction(snapshotModel),
    createInitialPresentation(snapshotModel),
    DefaultActionGroup(), // Not used - we override showActionGroupPopup with custom popup
    "UnityProfilerChart",
    ActionToolbar.DEFAULT_MINIMUM_BUTTON_SIZE
) {

    init {
        snapshotModel.fetchingMode.advise(snapshotModel.lifetime.intersect(lifetime)) { mode ->
            presentation.text = if (mode == FetchingMode.Auto) UnityUIBundle.message("unity.profiler.integration.auto") else UnityUIBundle.message("unity.profiler.integration.manual")
            presentation.icon = if (mode == FetchingMode.Auto) AllIcons.General.InspectionsOK else AllIcons.General.Refresh

            revalidate()
            repaint()
        }
    }

    override fun showActionGroupPopup(actionGroup: ActionGroup, event: AnActionEvent) {
        UnityProfilerModePopup.show(snapshotModel, this)
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
        val foreground = if (isEnabled) foreground else UIUtil.getInactiveTextColor()
        return TextIcon(baseIcon, text, font, getFontMetrics(font), foreground)
    }

    companion object {
        private fun createMainModeAction(viewModel: UnityProfilerSnapshotModel) = object : DumbAwareAction() {
            override fun actionPerformed(e: AnActionEvent) {
                viewModel.requestNewSnapshot()
            }

            override fun update(e: AnActionEvent) {
                val mode = viewModel.fetchingMode.valueOrNull ?: FetchingMode.Auto
                e.presentation.text = if (mode == FetchingMode.Auto) UnityUIBundle.message("unity.profiler.integration.auto") else UnityUIBundle.message("unity.profiler.integration.manual")
                e.presentation.icon = if (mode == FetchingMode.Auto) AllIcons.General.InspectionsOK else AllIcons.General.Refresh
            }

            override fun getActionUpdateThread() = ActionUpdateThread.EDT
        }

        private fun createInitialPresentation(viewModel: UnityProfilerSnapshotModel) = Presentation().apply {
            val mode = viewModel.fetchingMode.valueOrNull ?: FetchingMode.Auto
            text = if (mode == FetchingMode.Auto) UnityUIBundle.message("unity.profiler.integration.auto") else UnityUIBundle.message("unity.profiler.integration.manual")
            icon = if (mode == FetchingMode.Auto) AllIcons.General.InspectionsOK else AllIcons.General.Refresh
        }
    }
}
