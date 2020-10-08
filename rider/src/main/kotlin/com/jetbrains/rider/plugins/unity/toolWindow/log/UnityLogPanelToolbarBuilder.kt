package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.*
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.ui.RiderAction
import icons.UnityIcons
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

    fun createLeftToolbar(model: UnityLogPanelModel): JPanel {
        fun createType(type: LogEventType) = object : ToggleAction("Show/Hide ${type}s", "", type.getIcon()) {
            override fun isSelected(e: AnActionEvent) = model.typeFilters.getShouldBeShown(type)
            override fun setSelected(e: AnActionEvent, value: Boolean) = model.typeFilters.setShouldBeShown(type, value)

            override fun update(e: AnActionEvent) {
                if (isSelected(e))
                    e.presentation.text = "Hide ${type}s"
                else
                    e.presentation.text = "Show ${type}s"
                super.update(e)
            }
        }

        fun createMode(mode: LogEventMode) = object : ToggleAction("Show/Hide '$mode' mode", "", mode.getIcon()) {
            override fun isSelected(e: AnActionEvent) = model.modeFilters.getShouldBeShown(mode)
            override fun setSelected(e: AnActionEvent, value: Boolean) = model.modeFilters.setShouldBeShown(mode, value)

            override fun update(e: AnActionEvent) {
                if (isSelected(e))
                    e.presentation.text = "Hide '$mode' mode"
                else
                    e.presentation.text = "Show '$mode' mode"
                super.update(e)
            }
        }

        fun collapseAll() = object : ToggleAction("Collapse Similar Items", "", AllIcons.Actions.Collapseall) {
            override fun isSelected(e: AnActionEvent) = model.mergeSimilarItems.value
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.mergeSimilarItems.set(value)
            }
        }

        fun autoscroll() = object : ToggleAction("Autoscroll", "", AllIcons.RunConfigurations.Scroll_down) {
            override fun isSelected(e: AnActionEvent) = model.autoscroll.value
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.autoscroll.set(value)
                model.events.onAutoscrollChanged.fire(value)
            }
        }

        fun createBeforePlay() = object : ToggleAction("Messages Before Last Play in Unity", "", UnityIcons.LogView.FilterBeforePlay) {
            override fun isSelected(e: AnActionEvent) = model.timeFilters.getShouldBeShownBeforePlay()
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.timeFilters.setShowBeforePlay(value)
            }
        }

        fun createBeforeInit() = object : ToggleAction("Messages Before Last Domain Reload", "",  UnityIcons.LogView.FilterBeforeRefresh) {
            override fun isSelected(e: AnActionEvent) = model.timeFilters.getShouldBeShownBeforeInit()
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.timeFilters.setShowBeforeLastBuild(value)
            }
        }

        val actionGroup = DefaultActionGroup().apply {
            addSeparator("Mode Filters")
            add(createMode(LogEventMode.Edit))
            add(createMode(LogEventMode.Play))
            addSeparator("Type Filters")
            add(createType(LogEventType.Error))
            add(createType(LogEventType.Warning))
            add(createType(LogEventType.Message))
            addSeparator("Time Filters")
            add(createBeforePlay())
            add(createBeforeInit())
            addSeparator("Other")
            add(collapseAll())
            add(autoscroll())
            add(RiderAction("Clear", AllIcons.Actions.GC) { model.events.clear() })
        }

        return create(actionGroup, BorderLayout.WEST, false)
    }
}