package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.solution

class RiderUnityOpenEditorConsoleLogViewAction : RiderUnityLogViewAction("Open Unity Editor Log", "", UnityIcons.Unity.UnityEdit) {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.openEditorConsole.start(Unit)
    }
}

class RiderUnityOpenPlayerConsoleLogViewAction : RiderUnityLogViewAction("Open Unity Play Log", "", UnityIcons.Unity.UnityPlay) {

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.rdUnityModel.openPlayerConsole.start(Unit)
    }
}