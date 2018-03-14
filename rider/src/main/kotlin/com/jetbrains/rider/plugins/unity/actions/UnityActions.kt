package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons

class PlayInUnityAction(val host:UnityHost) : ToggleAction("Play/Edit", "Change Play/Edit mode in Unity", UnityIcons.PlayInUnity) {

    override fun isSelected(e: AnActionEvent?):Boolean {
        return host.play.value
    }
    override fun setSelected(e: AnActionEvent?, value: Boolean) {
        val project = e?.project?: return
        UnityHost.CallBackendPlay(project, value)
    }
    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = host.sessionInitialized.value
        super.update(e)
    }
}

class PauseInUnityAction(val host:UnityHost) : ToggleAction("Pause/Resume", "Pause/Resume play in Unity", UnityIcons.PauseInUnity) {
    override fun isSelected(e: AnActionEvent?):Boolean {
        return host.pause.value
    }
    override fun setSelected(e: AnActionEvent?, value: Boolean) {
        val project = e?.project?: return
        UnityHost.CallBackendPause(project, value)
    }

    override fun update(e: AnActionEvent) {
        e.presentation.isEnabled = host.play.value && host.sessionInitialized.value
        super.update(e)
    }
}

class StepInUnityAction(val host:UnityHost) : AnAction("Step", "Perform a single frame step.", UnityIcons.StepInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        UnityHost.CallBackendStep(project)
    }

    override fun update(e: AnActionEvent?) {
        e?.presentation?.isEnabled = host.play.value && host.sessionInitialized.value
        super.update(e)
    }
}