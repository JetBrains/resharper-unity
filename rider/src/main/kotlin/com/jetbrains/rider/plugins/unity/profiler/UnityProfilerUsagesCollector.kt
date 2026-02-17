package com.jetbrains.rider.plugins.unity.profiler

import com.intellij.internal.statistic.eventLog.EventLogGroup
import com.intellij.internal.statistic.eventLog.events.EventFields
import com.intellij.internal.statistic.service.fus.collectors.CounterUsagesCollector
import com.intellij.openapi.project.Project

/**
 * FUS collector for Unity Profiler telemetry events.
 *
 * This is a stateless singleton that only defines and logs events.
 * Per-project session state is managed by UnityProfilerUsagesDaemon.
 */
object UnityProfilerUsagesCollector : CounterUsagesCollector() {

    private val GROUP = EventLogGroup("dotnet.unity.profiler", 5)

    // Existing events (previously in C# UnityProfilerInfoCollector)
    private val NAVIGATED_FROM_UNITY_PROFILER = GROUP.registerEvent("navigated_from_unity_profiler")
    private val SELECTED_FRAME_IN_UNITY_PROFILER = GROUP.registerEvent("selected_frame_in_unity_profiler", EventFields.RoundedInt("samples_count"))
    private val SNAPSHOT_FETCHED = GROUP.registerEvent("snapshot_fetched", EventFields.RoundedInt("samples_count"), EventFields.DurationMs)
    private val NAVIGATE_GUTTER_TO_PARENT_CALL = GROUP.registerEvent("navigate_gutter_to_parent_call")

    // New frontend events
    // Adoption: Unity Profiler tool window opened
    private val TOOL_WINDOW_OPENED = GROUP.registerEvent("tool_window_opened")

    // Engagement: User navigates from profiler tree/frame to source code
    private val NAVIGATE_TREE_TO_CODE = GROUP.registerEvent("navigate_tree_to_code")

    // Engagement: User opens profiler tool window from gutter popup
    private val NAVIGATE_GUTTER_TO_PROFILER = GROUP.registerEvent("navigate_gutter_to_profiler")

    // Aggregated session statistics - logged when tool window closes or session ends
    private val SESSION_GUTTER_CLICKS = GROUP.registerEvent("session_gutter_clicks", EventFields.RoundedInt("count"))
    private val SESSION_GRAPH_CLICKS = GROUP.registerEvent("session_graph_clicks", EventFields.RoundedInt("count"))
    private val SESSION_TREE_INTERACTIONS = GROUP.registerEvent("session_tree_interactions", EventFields.RoundedInt("count"))

    override fun getGroup(): EventLogGroup = GROUP

    // Methods for existing events (to be called from C# backend or Kotlin frontend)
    fun logNavigatedFromUnityProfiler(project: Project) {
        NAVIGATED_FROM_UNITY_PROFILER.log(project)
    }

    fun logSelectedFrameInUnityProfiler(project: Project, samplesCount: Int) {
        SELECTED_FRAME_IN_UNITY_PROFILER.log(project, samplesCount)
    }

    fun logSnapshotFetched(project: Project, samplesCount: Int, snapshotLoadDurationMs: Long) {
        SNAPSHOT_FETCHED.log(project, samplesCount, snapshotLoadDurationMs)
    }

    fun logNavigateGutterToParentCall(project: Project) {
        NAVIGATE_GUTTER_TO_PARENT_CALL.log(project)
    }

    // Methods for new frontend events
    fun logToolWindowOpened(project: Project) {
        TOOL_WINDOW_OPENED.log(project)
    }

    fun logNavigateTreeToCode(project: Project) {
        NAVIGATE_TREE_TO_CODE.log(project)
    }

    fun logNavigateGutterToProfiler(project: Project) {
        NAVIGATE_GUTTER_TO_PROFILER.log(project)
    }

    /**
     * Logs accumulated session statistics.
     * Called by UnityProfilerUsagesDaemon when the project closes.
     */
    internal fun logSessionInteractions(
        project: Project,
        gutterClicks: Int,
        graphClicks: Int,
        treeInteractions: Int
    ) {
        if (gutterClicks > 0) {
            SESSION_GUTTER_CLICKS.log(project, gutterClicks)
        }
        if (graphClicks > 0) {
            SESSION_GRAPH_CLICKS.log(project, graphClicks)
        }
        if (treeInteractions > 0) {
            SESSION_TREE_INTERACTIONS.log(project, treeInteractions)
        }
    }
}
