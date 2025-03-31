package com.jetbrains.rider.plugins.unity.actions

import com.intellij.ide.actions.ShowSettingsUtilImpl
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.DumbAwareAction
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.plugins.unity.ui.profilerIntegration.unitySettingsPageId
import com.jetbrains.rider.projectView.solution

class EnableAutoProfilerSnapshotFetchingSettings : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        project.solution.frontendBackendModel.unityApplicationSettings.profilerSnapshotFetchingSettings.fire(1) // 1 - stands for autoFetch

        ShowSettingsUtilImpl.showSettingsDialog(project,
                                                unitySettingsPageId,
                                                UnityUIBundle.message("unity.profiler.integration.widget.setting.filter"))
    }
}