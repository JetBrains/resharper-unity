package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.intellij.openapi.project.DumbAware
import com.jetbrains.rd.util.reactive.valueOrDefault

class PlayInUnityAction : ToggleAction(), DumbAware {

    override fun isSelected(e: AnActionEvent):Boolean {
        val model = e.getFrontendBackendModel() ?: return false
        return model.play.valueOrDefault(false)
    }

    override fun setSelected(e: AnActionEvent, value: Boolean) {
        e.getFrontendBackendModel()?.play?.set(value)
    }

    override fun update(e: AnActionEvent) {
        e.handleUpdateForUnityConnection()
        super.update(e)
    }
}

class PauseInUnityAction : ToggleAction(), DumbAware {

    override fun isSelected(e: AnActionEvent):Boolean {
        val model = e.getFrontendBackendModel() ?: return false
        return model.pause.valueOrDefault(false)
    }

    override fun setSelected(e: AnActionEvent, value: Boolean) {
        e.getFrontendBackendModel()?.pause?.set(value)
    }

    override fun update(e: AnActionEvent) {
        e.handleUpdateForUnityConnection {
            it.play.valueOrDefault(false)
        }
        super.update(e)
    }
}

class StepInUnityAction : AnAction(), DumbAware {

    override fun actionPerformed(e: AnActionEvent) {
        e.getFrontendBackendModel()?.step?.fire(Unit)
    }

    override fun update(e: AnActionEvent) {
        e.handleUpdateForUnityConnection {
            it.play.valueOrDefault(false)
        }
    }
}
