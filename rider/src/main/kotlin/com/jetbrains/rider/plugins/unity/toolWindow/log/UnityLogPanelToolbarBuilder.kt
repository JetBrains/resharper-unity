package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.*
import com.jetbrains.rider.plugins.unity.UnityHost
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.RdLogEventMode
import com.jetbrains.rider.plugins.unity.RdLogEventType
import com.jetbrains.rider.plugins.unity.actions.*
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
        fun createType(type: RdLogEventType) = object : ToggleAction("Show/Hide ${type}s", "", type.getIcon()) {
            override fun isSelected(e: AnActionEvent?) = model.typeFilters.getShouldBeShown(type)
            override fun setSelected(e: AnActionEvent?, value: Boolean) = model.typeFilters.setShouldBeShown(type, value)
        }

        fun createMode(mode: RdLogEventMode) = object : ToggleAction("Show/Hide '$mode' mode", "", mode.getIcon()) {
            override fun isSelected(e: AnActionEvent?) = model.modeFilters.getShouldBeShown(mode)
            override fun setSelected(e: AnActionEvent?, value: Boolean) = model.modeFilters.setShouldBeShown(mode, value)
        }

        val actionGroup = DefaultActionGroup().apply {
            addSeparator("Mode filters")
            add(createMode(RdLogEventMode.Edit))
            add(createMode(RdLogEventMode.Play))
            addSeparator("Type filters")
            add(createType(RdLogEventType.Error))
            add(createType(RdLogEventType.Warning))
            add(createType(RdLogEventType.Message))
            addSeparator("Other")
            add(RiderAction("Clear", AllIcons.Actions.GC) { model.events.clear() })
            addAll(consoleActionsList)
            add(mainSplitterToggleAction)
            add(UnityPluginShowSettingsAction())
        }

        return create(actionGroup, BorderLayout.WEST, false)
    }
}