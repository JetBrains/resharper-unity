package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.ide.actions.ShowSettingsUtilImpl
import com.intellij.openapi.options.SearchableConfigurable
import com.intellij.openapi.options.ShowSettingsUtil
import com.intellij.openapi.options.ex.ConfigurableVisitor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.util.concurrency.annotations.RequiresEdt

const val UNITY_FOR_AGENTS_PAGE_ID: String = "preferences.build.unityPlugin.agents"

internal const val AGENT_SKILLS_SETTINGS_ID: String = "com.intellij.aiagentextensions.skills.settings"

const val UNITY_FOR_AGENTS_HELP_TOPIC: String = "Settings_Unity_Engine_Unity_Agents_Setup"

@NlsSafe
internal const val UNITY_SKILLS_SEARCH: String = "Unity"

@RequiresEdt
fun openUnityForAgentsPage(project: Project) {
    ShowSettingsUtilImpl.showSettingsDialog(project, UNITY_FOR_AGENTS_PAGE_ID, null)
}

@RequiresEdt
fun openUnitySkillsPage(project: Project) {
    val settings = ShowSettingsUtil.getInstance() as? ShowSettingsUtilImpl
    if (settings == null) {
        openUnityForAgentsPage(project)
        return
    }
    settings.showSettingsDialogLazily(project, null) { groups ->
        val page = ConfigurableVisitor.findById(AGENT_SKILLS_SETTINGS_ID, groups)
        if (page != null) {
            (page as? SearchableConfigurable)?.enableSearch(UNITY_SKILLS_SEARCH)?.run()
            page
        }
        else {
            ConfigurableVisitor.findById(UNITY_FOR_AGENTS_PAGE_ID, groups)
        }
    }
}
