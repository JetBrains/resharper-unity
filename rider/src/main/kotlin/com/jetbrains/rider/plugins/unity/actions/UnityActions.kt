package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.RdUnityHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class PlayInUnityAction(val rdUnityHost: RdUnityHost) : ToggleAction("Play/Edit", "Change Play/Edit mode in Unity", UnityIcons.PlayInUnity) {

    override fun isSelected(e: AnActionEvent?):Boolean {
        return rdUnityHost.play.value
    }
    override fun setSelected(e: AnActionEvent?, value: Boolean) {
        val project = e?.project?: return
        RdUnityHost.CallBackendPlay(project, value)
    }
    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = projectCustomDataHost.isConnected.value
        super.update(e)
    }
}

class PauseInUnityAction(val rdUnityHost: RdUnityHost) : ToggleAction("Pause/Resume", "Pause/Resume play in Unity", UnityIcons.PauseInUnity) {
    override fun isSelected(e: AnActionEvent?):Boolean {
        return rdUnityHost.pause.value
    }
    override fun setSelected(e: AnActionEvent?, value: Boolean) {
        val project = e?.project?: return
        RdUnityHost.CallBackendPause(project, value)
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = rdUnityHost.play.value
        super.update(e)
    }
}

class StepInUnityAction(val rdUnityHost: RdUnityHost) : AnAction("Step", "Perform a single frame step.", UnityIcons.StepInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        RdUnityHost.CallBackendStep(project)
    }

    override fun update(e: AnActionEvent?) {
        e?.presentation?.isEnabled = rdUnityHost.play.value
        super.update(e)
    }
}