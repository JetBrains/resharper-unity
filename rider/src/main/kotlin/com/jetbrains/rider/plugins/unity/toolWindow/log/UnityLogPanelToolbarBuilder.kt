package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.model.LogEventMode
import com.jetbrains.rider.model.LogEventType
import com.jetbrains.rider.plugins.unity.actions.UnityPluginShowSettingsAction
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.ui.RiderAction
import java.awt.BorderLayout
import javax.swing.JPanel

object UnityLogPanelToolbarBuilder {
    private fun create(actionGroup: ActionGroup, layout: String, horizontal: Boolean): JPanel {
        val component = ActionManager.getInstance()
            .createActionToolbar(UnityToolWindowFactory.ACTION_PLACE, actionGroup, horizontal)
            .component

        return JPanel(BorderLayout()).apply { add(component, layout) }
    }

    fun createTopToolbar(): JPanel {
        return create(ActionGroup.EMPTY_GROUP, BorderLayout.NORTH, true)
    }

    fun createLeftToolbar(model: UnityLogPanelModel, mainSplitterToggleAction: DumbAwareAction, consoleActionsList : List<AnAction>): JPanel {
        fun createType(type: LogEventType) = object : ToggleAction("Show/Hide ${type}s", "", type.getIcon()) {
            override fun isSelected(e: AnActionEvent) = model.typeFilters.getShouldBeShown(type)
            override fun setSelected(e: AnActionEvent, value: Boolean) = model.typeFilters.setShouldBeShown(type, value)
        }

        fun createMode(mode: LogEventMode) = object : ToggleAction("Show/Hide '$mode' mode", "", mode.getIcon()) {
            override fun isSelected(e: AnActionEvent) = model.modeFilters.getShouldBeShown(mode)
            override fun setSelected(e: AnActionEvent, value: Boolean) = model.modeFilters.setShouldBeShown(mode, value)
        }

        fun collapseall() = object : ToggleAction("Collapse similar items", "", AllIcons.Actions.Collapseall) {
            override fun isSelected(e: AnActionEvent) = model.mergeSimilarItems.value
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.mergeSimilarItems.set(value)
            }
        }

        val actionGroup = DefaultActionGroup().apply {
            addSeparator("Mode filters")
            add(createMode(LogEventMode.Edit))
            add(createMode(LogEventMode.Play))
            addSeparator("Type filters")
            add(createType(LogEventType.Error))
            add(createType(LogEventType.Warning))
            add(createType(LogEventType.Message))
            addSeparator("Other")
            add(collapseall())
            add(RiderAction("Clear", AllIcons.Actions.GC) { model.events.clear() })
            addAll(consoleActionsList)
            add(mainSplitterToggleAction)
            add(ActionManager.getInstance().getAction(UnityPluginShowSettingsAction.actionId))
        }

        return create(actionGroup, BorderLayout.WEST, false)
    }
}