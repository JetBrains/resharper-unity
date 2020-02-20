package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rd.util.reactive.valueOrDefault

class PlayInUnityAction : ToggleAction() {

    override fun isSelected(e: AnActionEvent):Boolean {
        val model = e.getModel() ?: return false
        return model.play.valueOrDefault(false)
    }

    override fun setSelected(e: AnActionEvent, value: Boolean) {
        e.getModel()?.play?.set(value)
    }

    override fun update(e: AnActionEvent) {
        e.handleUpdateForUnityConnection()
        super.update(e)
    }
}

class PauseInUnityAction : ToggleAction() {

    override fun isSelected(e: AnActionEvent):Boolean {
        val model = e.getModel() ?: return false
        return model.pause.valueOrDefault(false)
    }

    override fun setSelected(e: AnActionEvent, value: Boolean) {
        e.getModel()?.pause?.set(value)
    }

    override fun update(e: AnActionEvent) {
        e.handleUpdateForUnityConnection {
            it.play.valueOrDefault(false)
        }
        super.update(e)
    }
}

class StepInUnityAction : AnAction() {

    override fun actionPerformed(e: AnActionEvent) {
        e.getModel()?.step?.fire(Unit)
    }

    override fun update(e: AnActionEvent) {
        e.handleUpdateForUnityConnection {
            it.play.valueOrDefault(false)
        }
    }
}
