package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.AnActionEvent
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import com.jetbrains.rider.projectView.solution

class RefreshInUnityAction : AnAction(UnityIcons.AttachEditorDebugConfiguration) {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project?: return

        project.solution.customData.data["UNITY_Refresh"] = "true";
    }
}