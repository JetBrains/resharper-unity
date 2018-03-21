package com.jetbrains.rider.plugins.unity.actions

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.ToggleAction
import com.jetbrains.rider.UnityReferenceDiscoverer
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.util.idea.tryGetComponent

class PlayInUnityAction() : ToggleAction("Play/Edit", "Change Play/Edit mode in Unity", UnityIcons.Actions.Execute) {

    override fun isSelected(e: AnActionEvent):Boolean {
        val projectCustomDataHost = e.getHost() ?: return false
        val play = projectCustomDataHost.play.value
        return play!=null && play
    }
    override fun setSelected(e: AnActionEvent?, value: Boolean) {
        val project = e?.project?: return
        ProjectCustomDataHost.CallBackendPlay(project, value)
    }
    override fun update(e: AnActionEvent) {
        if (!e.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true

        val projectCustomDataHost = e.getHost() ?: return
        e.presentation.isEnabled = projectCustomDataHost.sessionInitialized.value
        super.update(e)
    }
}

class PauseInUnityAction() : ToggleAction("Pause/Resume", "Pause/Resume play in Unity", UnityIcons.Actions.Pause) {
    override fun isSelected(e: AnActionEvent):Boolean {
        val projectCustomDataHost = e.getHost() ?: return false
        return projectCustomDataHost.pause.value
    }
    override fun setSelected(e: AnActionEvent?, value: Boolean) {
        val project = e?.project?: return
        ProjectCustomDataHost.CallBackendPause(project, value)
    }

    override fun update(e: AnActionEvent) {
        if (!e.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true

        val projectCustomDataHost = e.getHost() ?: return
        val play = projectCustomDataHost.play.value;
        e.presentation.isEnabled =  play!= null && play && projectCustomDataHost.sessionInitialized.value
        super.update(e)
    }
}

class StepInUnityAction() : AnAction(UnityIcons.Actions.Step) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        ProjectCustomDataHost.CallBackendStep(project)
    }

    override fun update(e: AnActionEvent) {
        if (!e.isUnityProject()) {
            e.presentation.isVisible = false
            return
        }

        e.presentation.isVisible = true

        val projectCustomDataHost = e.getHost() ?: return
        val play = projectCustomDataHost.play.value
        e.presentation.isEnabled = play!=null && play && projectCustomDataHost.sessionInitialized.value
        super.update(e)
    }
}

fun AnActionEvent.getHost(): ProjectCustomDataHost? {
    val project = project?: return null
    return project.tryGetComponent()
}

fun AnActionEvent.isUnityProject(): Boolean {
    val project = this.project ?: return false
    val discoverer = project.tryGetComponent<UnityReferenceDiscoverer>() ?: return false
    return discoverer.isUnityGeneratedProject
}