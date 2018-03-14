package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.getComponent

class PlayInUnityAction() : ToggleAction(null, null, UnityIcons.PlayInUnity) {
    override fun isSelected(e: AnActionEvent):Boolean {
        val project = e.project
        val result = project != null && project.getComponent<ProjectCustomDataHost>().play.value
        return result
    }
    override fun setSelected(e: AnActionEvent, value: Boolean) {
        val project = e.project?: return
        ProjectCustomDataHost.CallBackendPlay(project, value)
    }
    override fun update(e: AnActionEvent) {
        val project = e.project
        e.presentation.isEnabled =  project != null && project.getComponent<ProjectCustomDataHost>().sessionInitialized.value
    }
}

class PauseInUnityAction() : ToggleAction(null, null, UnityIcons.PauseInUnity) {
    override fun isSelected(e: AnActionEvent):Boolean {
        val project = e.project
        return project != null && project.getComponent<ProjectCustomDataHost>().pause.value
    }
    override fun setSelected(e: AnActionEvent, value: Boolean) {
        val project = e.project?: return
        ProjectCustomDataHost.CallBackendPause(project, value)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        e.presentation.isEnabled = project != null && project.getComponent<ProjectCustomDataHost>().play.value && project.getComponent<ProjectCustomDataHost>().sessionInitialized.value
        super.update(e)
    }
}

class StepInUnityAction() : AnAction(UnityIcons.StepInUnity) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return
        ProjectCustomDataHost.CallBackendStep(project)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project
        e.presentation.isEnabled = project != null && project.getComponent<ProjectCustomDataHost>().play.value && project.getComponent<ProjectCustomDataHost>().sessionInitialized.value
    }
}