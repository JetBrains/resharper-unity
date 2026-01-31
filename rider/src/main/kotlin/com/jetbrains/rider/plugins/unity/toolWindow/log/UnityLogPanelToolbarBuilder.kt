package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.ActionGroup
import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.LogEventMode
import com.jetbrains.rider.plugins.unity.model.LogEventType
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

        fun getLocalizedName(eventType: LogEventType): String {
            return when (eventType) {
                LogEventType.Error -> UnityBundle.message("logEventType.errors")
                LogEventType.Warning -> UnityBundle.message("logEventType.warning")
                LogEventType.Message -> UnityBundle.message("logEventType.messages")
            }
        }

        fun createType(type: LogEventType) = object : ToggleAction(UnityBundle.message("show.hide", getLocalizedName(type)), "",
                                                                   type.getIcon()) {
            override fun isSelected(e: AnActionEvent) = model.typeFilters.getShouldBeShown(type)
            override fun setSelected(e: AnActionEvent, value: Boolean) = model.typeFilters.setShouldBeShown(type, value)
            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

            override fun update(e: AnActionEvent) {
                if (isSelected(e))
                    e.presentation.text = UnityBundle.message("hide", getLocalizedName(type))
                else
                    e.presentation.text = UnityBundle.message("show", getLocalizedName(type))
                super.update(e)
            }
        }

        fun createMode(mode: LogEventMode) = object : ToggleAction(UnityBundle.message("action.show.hide.mode.text", mode), "",
                                                                   mode.getIcon()) {
            override fun isSelected(e: AnActionEvent) = model.modeFilters.getShouldBeShown(mode)
            override fun setSelected(e: AnActionEvent, value: Boolean) = model.modeFilters.setShouldBeShown(mode, value)
            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT

            override fun update(e: AnActionEvent) {
                if (isSelected(e))
                    e.presentation.text = UnityBundle.message("action.hide.mode.text", mode)
                else
                    e.presentation.text = UnityBundle.message("action.show.mode.text", mode)
                super.update(e)
            }
        }

        fun collapseAll() = object : ToggleAction(UnityBundle.message("action.collapse.similar.items.text"), "",
                                                  AllIcons.Actions.Collapseall) {
            override fun isSelected(e: AnActionEvent) = model.mergeSimilarItems.value
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.mergeSimilarItems.set(value)
            }

            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
        }

        fun autoscroll() = object : ToggleAction(UnityBundle.message("action.autoscroll.text"), "",
                                                 AllIcons.RunConfigurations.Scroll_down) {
            override fun isSelected(e: AnActionEvent) = model.autoscroll.value
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.autoscroll.set(value)
                model.events.onAutoscrollChanged.fire(value)
            }

            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
        }

        fun createBeforePlay() = object : ToggleAction(UnityBundle.message("action.messages.before.last.play.in.unity.text"), "",
                                                       UnityIcons.LogView.FilterBeforePlay) {
            override fun isSelected(e: AnActionEvent) = model.timeFilters.getShouldBeShownBeforePlay()
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.timeFilters.setShowBeforePlay(value)
            }

            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
        }

        fun createBeforeInit() = object : ToggleAction(UnityBundle.message("action.messages.before.last.domain.reload.text"), "",
                                                       UnityIcons.LogView.FilterBeforeRefresh) {
            override fun isSelected(e: AnActionEvent) = model.timeFilters.getShouldBeShownBeforeInit()
            override fun setSelected(e: AnActionEvent, value: Boolean) {
                model.timeFilters.setShowBeforeLastBuild(value)
            }

            override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
        }

        val actionGroup = DefaultActionGroup().apply {
            addSeparator(UnityBundle.message("separator.mode.filters"))
            add(createMode(LogEventMode.Edit))
            add(createMode(LogEventMode.Play))
            addSeparator(UnityBundle.message("separator.type.filters"))
            add(createType(LogEventType.Error))
            add(createType(LogEventType.Warning))
            add(createType(LogEventType.Message))
            addSeparator(UnityBundle.message("separator.time.filters"))
            add(createBeforePlay())
            add(createBeforeInit())
            addSeparator(UnityBundle.message("separator.other"))
            add(collapseAll())
            add(autoscroll())
            add(RiderAction(UnityBundle.message("action.clear.text"), AllIcons.Actions.GC) { model.events.clear() })
        }

        return create(actionGroup, BorderLayout.WEST, false)
    }
}