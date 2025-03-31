package com.jetbrains.rider.plugins.unity.ui.profilerIntegration

import com.intellij.ide.HelpTooltip
import com.intellij.ide.actions.ShowSettingsUtilImpl
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.actionSystem.impl.ActionButton
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.markup.InspectionWidgetActionProvider
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.languages.fileTypes.csharp.CSharpFileType
import com.jetbrains.rider.plugins.unity.hasUnityReference
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.ProfilerSnapshotRequest
import com.jetbrains.rider.plugins.unity.model.SnapshotStatus
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import com.jetbrains.rider.projectView.solution

/**
 * Provides actions for the Unity Profiler integration widget in the editor.
 * Creates and manages the action group that appears in the editor's inspection widget area.
 */
class ProfilerIntegrationWidgetActionProvider : InspectionWidgetActionProvider {
    override fun createAction(editor: Editor): AnAction? {
        return DefaultActionGroup(Separator.create(),
                                  ActionManager.getInstance().getAction("ProfilerIntegrationWidgetAction"),
                                  Separator.create())
    }
}

internal const val unitySettingsPageId = "preferences.build.unityPlugin"

/**
 * Action that handles the Unity Profiler integration widget functionality.
 * Manages the widget's state and appearance based on the Unity connection status and profiling data availability.
 */
class ProfilerIntegrationWidgetAction : AnAction() {
    override fun getActionUpdateThread(): ActionUpdateThread = ActionUpdateThread.EDT

    /**
     * Handles the action when the widget is clicked.
     * Attempts to update the profiler snapshot data if available.
     */
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val model = project.solution.frontendBackendModel.frontendBackendProfilerModel

        model.profilerSnapshotStatus.valueOrNull?.let {
            model.updateUnityProfilerSnapshotData.fire(ProfilerSnapshotRequest(it.frameIndex, it.threadIndex))
        }
    }

    /**
     * Updates the widget's state and appearance based on current conditions.
     * Handles different states of Unity connection and profiling data availability.
     */
    override fun update(e: AnActionEvent) {
        val project = e.project
        if (project == null || !project.hasUnityReference.value) {
            e.presentation.isEnabledAndVisible = false
            return
        }

        val editor = e.getData(CommonDataKeys.EDITOR) ?: return
        if (editor.isViewer) {
            e.presentation.isEnabledAndVisible = false
            return
        }

        if (!isUnityScriptFile(project, editor)) {
            e.presentation.isEnabledAndVisible = false
            return
        }

        val model = project.solution.frontendBackendModel
        val profilerStatus = model.frontendBackendProfilerModel.profilerSnapshotStatus.valueOrNull

        fun refreshPresentationState(stateNoDataAvailable: ProfilerActionStateSetup) {
            updatePresentation(e.presentation, editor, stateNoDataAvailable)
        }

        if (!model.unityEditorConnected.valueOrDefault(false)) {
            refreshPresentationState(ProfilerActionStateDisconnected())
            e.presentation.isEnabledAndVisible = false //If it's not turned off, hide the widget.
            return
        }

        when (profilerStatus?.status) {
            null, SnapshotStatus.Disabled -> {
                refreshPresentationState(ProfilerActionStateDisabled())
                e.presentation.isEnabledAndVisible = false //If it's not turned off, hide the widget.
                return
            }
            SnapshotStatus.NoSnapshotDataAvailable -> refreshPresentationState(ProfilerActionStateNoDataAvailable())
            SnapshotStatus.HasNewSnapshotDataToFetch -> refreshPresentationState(ProfilerActionStateHasDataToFetch(profilerStatus))
            SnapshotStatus.SnapshotDataFetchingInProgress -> refreshPresentationState(ProfilerActionStateFetchingInProgress(profilerStatus))
            SnapshotStatus.SnapshotDataIsUpToDate -> refreshPresentationState(ProfilerActionStateDataIsUpToDate(profilerStatus))
        }
    }

    /**
     * Checks if the current file is a Unity script file.
     * @param project The current project
     * @param editor The current editor
     * @return true if the file is a Unity C# script, false otherwise
     */
    private fun isUnityScriptFile(project: Project, editor: Editor): Boolean {
        return project.isUnityProject.value &&
               editor.virtualFile?.fileType == CSharpFileType
    }

    /**
     * Updates the widget's presentation based on the current state.
     * @param presentation The presentation to update
     * @param editor The current editor
     * @param snapshotStateSetup The state setup containing icon and tooltip information
     */
    private fun updatePresentation(
        presentation: Presentation,
        editor: Editor,
        snapshotStateSetup: ProfilerActionStateSetup,
    ) {
        presentation.icon = snapshotStateSetup.icon
        val tooltip = HelpTooltip()
            .setTitle(snapshotStateSetup.tooltipTitle)
            .setDescription(snapshotStateSetup.tooltipDescription)
            .setLink(UnityUIBundle.message("unity.profiler.integration.widget.setting")) {
                ShowSettingsUtilImpl.showSettingsDialog(editor.project,
                                                        unitySettingsPageId,
                                                        UnityUIBundle.message("unity.profiler.integration.widget.setting.filter"))
            }
        presentation.putClientProperty(ActionButton.CUSTOM_HELP_TOOLTIP, tooltip)
    }
}
