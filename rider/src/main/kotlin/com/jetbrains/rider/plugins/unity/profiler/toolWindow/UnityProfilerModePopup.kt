package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.icons.AllIcons
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.ui.popup.PopupStep
import com.intellij.openapi.ui.popup.util.BaseListPopupStep
import com.intellij.ui.SimpleColoredComponent
import com.intellij.ui.SimpleTextAttributes
import com.intellij.ui.popup.list.ListPopupImpl
import com.intellij.util.ui.JBUI
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerSnapshotModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import java.awt.BorderLayout
import java.awt.Component
import javax.swing.JComponent
import javax.swing.JPanel
import javax.swing.ListCellRenderer

internal data class ModeOption(
    val title: String,
    val description: String,
    val mode: FetchingMode
)

internal object UnityProfilerModePopup {
    private val modeOptions = listOf(
        ModeOption(
            UnityUIBundle.message("unity.profiler.integration.auto"),
            UnityUIBundle.message("unity.profiler.integration.auto.description"),
            FetchingMode.Auto
        ),
        ModeOption(
            UnityUIBundle.message("unity.profiler.integration.manual"),
            UnityUIBundle.message("unity.profiler.integration.manual.description"),
            FetchingMode.Manual
        )
    )

    fun show(snapshotModel: UnityProfilerSnapshotModel, owner: JComponent) {
        val step = object : BaseListPopupStep<ModeOption>(null, modeOptions) {
            override fun onChosen(selectedValue: ModeOption, finalChoice: Boolean): PopupStep<*>? {
                snapshotModel.fetchingMode.set(selectedValue.mode)
                return FINAL_CHOICE
            }

            override fun isSelectable(value: ModeOption): Boolean = true
        }

        val popup = JBPopupFactory.getInstance().createListPopup(step) as ListPopupImpl
        @Suppress("UNCHECKED_CAST")
        popup.list.cellRenderer = ModeOptionCellRenderer { snapshotModel.fetchingMode.valueOrNull } as ListCellRenderer<Any>
        popup.showUnderneathOf(owner)
    }
}

private class ModeOptionCellRenderer(
    private val currentModeProvider: () -> FetchingMode?
) : ListCellRenderer<ModeOption> {
    override fun getListCellRendererComponent(
        list: javax.swing.JList<out ModeOption>,
        value: ModeOption?,
        index: Int,
        isSelected: Boolean,
        cellHasFocus: Boolean
    ): Component {
        if (value == null) return JPanel()

        val panel = JPanel(BorderLayout()).apply {
            border = JBUI.Borders.empty(4, 8)
            background = if (isSelected) list.selectionBackground else list.background
        }

        val descriptionLabel = SimpleColoredComponent().apply {
            append(value.description, SimpleTextAttributes.GRAYED_ATTRIBUTES)
            font = JBUI.Fonts.smallFont()
            isOpaque = false
        }

        val titleLabel = SimpleColoredComponent().apply {
            val isCurrentMode = currentModeProvider() == value.mode
            if (isCurrentMode) {
                icon = AllIcons.Actions.Checked
            }
            val attrs = if (isSelected) SimpleTextAttributes.SELECTED_SIMPLE_CELL_ATTRIBUTES else SimpleTextAttributes.REGULAR_ATTRIBUTES
            append(value.title, attrs)
            isOpaque = false
        }

        panel.add(descriptionLabel, BorderLayout.NORTH)
        panel.add(titleLabel, BorderLayout.CENTER)

        return panel
    }
}
