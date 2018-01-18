package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.application

class PlayInUnityAction() : AnAction("Play", "Enter Play mode in Unity", UnityIcons.PlayInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        ProjectCustomDataHost.CallBackendPlay(project)
    }
}

class PauseInUnityAction() : AnAction("Pause", "Pause play in Unity", UnityIcons.PauseInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        ProjectCustomDataHost.CallBackendPause(project)
    }
}

class ResumeInUnityAction() : AnAction("Resume", "Resume play in Unity", UnityIcons.PlayInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        ProjectCustomDataHost.CallBackendResume(project)
    }
}

class StopInUnityAction() : AnAction("Stop", "Stop play in Unity", UnityIcons.StopInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        ProjectCustomDataHost.CallBackendStop(project)
    }
}

class StepInUnityAction() : AnAction("Step", "Perform a single frame step.", UnityIcons.StepInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        ProjectCustomDataHost.CallBackendStep(project)
    }
}