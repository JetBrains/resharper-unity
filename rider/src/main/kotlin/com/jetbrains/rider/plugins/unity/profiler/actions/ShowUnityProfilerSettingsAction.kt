package com.jetbrains.rider.plugins.unity.profiler.actions

import com.intellij.icons.AllIcons
import com.intellij.ide.actions.ShowSettingsUtilImpl
import com.intellij.openapi.actionSystem.ActionUpdateThread
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle

class ShowUnityProfilerSettingsAction : DumbAwareAction(
    UnityUIBundle.message("unity.profiler.integration.widget.setting.filter"),
    null,
    AllIcons.General.Settings
) {
    companion object {
        private const val unitySettingsPageId = "preferences.build.unityPlugin"
    }

    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        ShowSettingsUtilImpl.showSettingsDialog(
            project,
            unitySettingsPageId,
            UnityUIBundle.message("unity.profiler.integration.widget.setting.filter")
        )
    }

    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.BGT
}
