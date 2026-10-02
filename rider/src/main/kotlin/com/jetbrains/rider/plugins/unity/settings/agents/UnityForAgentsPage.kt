package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.ide.actions.ShowSettingsUtilImpl
import com.intellij.openapi.project.Project
import com.intellij.util.concurrency.annotations.RequiresEdt

const val UNITY_FOR_AGENTS_PAGE_ID: String = "preferences.build.unityPlugin.agents"

const val UNITY_FOR_AGENTS_HELP_TOPIC: String = "Settings_Unity_Engine_Unity_Agents_Setup"

@RequiresEdt
fun openUnityForAgentsPage(project: Project) {
    ShowSettingsUtilImpl.showSettingsDialog(project, UNITY_FOR_AGENTS_PAGE_ID, null)
}
