package com.jetbrains.rider.plugins.unity.ui.profilerIntegration

import com.jetbrains.rider.plugins.unity.model.UnityProfilerSnapshotStatus
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import icons.UnityIcons
import javax.swing.Icon

/**
 * Base class for different states of the Unity Profiler integration widget.
 * Each state defines its own visual representation and tooltip information.
 */
sealed class ProfilerActionStateSetup {
    /** Icon to be displayed in the widget for the current state */
    abstract val icon: Icon

    /** Title of the tooltip shown when hovering over the widget */
    abstract val tooltipTitle: String

    /** Detailed description shown in the tooltip */
    abstract val tooltipDescription: String
}

/**
 * Represents the state when the Unity Editor is not connected to Rider.
 * In this state, profiling data cannot be collected or displayed.
 */
class ProfilerActionStateDisconnected : ProfilerActionStateSetup() {
    override val icon: Icon = UnityIcons.Toolbar.ToolbarDisconnected
    override val tooltipTitle: String = UnityUIBundle.message("unity.profiler.integration.widget.disconnected.tooltipTitle")
    override val tooltipDescription: String = UnityUIBundle.message("unity.profiler.integration.widget.disconnected.tooltipDescription")
}

/**
 * Represents the state when profiling is explicitly disabled in settings.
 * Users need to enable profiling in settings to use this feature.
 */
class ProfilerActionStateDisabled : ProfilerActionStateSetup() {
    override val icon: Icon = UnityIcons.Profiler.Disabled
    override val tooltipTitle: String = UnityUIBundle.message("unity.profiler.integration.widget.disabled.tooltipTitle")
    override val tooltipDescription: String = UnityUIBundle.message("unity.profiler.integration.widget.disabled.tooltipDescription")
}

/**
 * Represents the state when no profiling data is available from Unity.
 * This occurs when profiling is enabled but no data has been collected yet.
 */
class ProfilerActionStateNoDataAvailable : ProfilerActionStateSetup() {
    override val icon: Icon = UnityIcons.Profiler.NoDataAvailable
    override val tooltipTitle: String = UnityUIBundle.message("unity.profiler.integration.widget.noDataAvailable.tooltipTitle")
    override val tooltipDescription: String = UnityUIBundle.message("unity.profiler.integration.widget.noDataAvailable.tooltipDescription")
}

/**
 * Represents the state when new profiling data is available to be fetched from Unity.
 * @property snapshotInfo Information about the available profiler snapshot, including frame and thread details
 */
class ProfilerActionStateHasDataToFetch(
    private val snapshotInfo: UnityProfilerSnapshotStatus
) : ProfilerActionStateSetup() {
    override val icon: Icon = UnityIcons.Profiler.HasNewData
    override val tooltipTitle: String = UnityUIBundle.message("unity.profiler.integration.widget.hasDataToFetch.tooltipTitle")
    override val tooltipDescription: String = UnityUIBundle.message(
        "unity.profiler.integration.widget.hasDataToFetch.tooltipDescription",
        snapshotInfo.frameIndex,
        snapshotInfo.threadName
    )
}

/**
 * Represents the state when profiling data is currently being fetched from Unity.
 * @property snapshotInfo Information about the profiler snapshot being fetched, including frame and thread details
 */
class ProfilerActionStateFetchingInProgress(
    private val snapshotInfo: UnityProfilerSnapshotStatus
) : ProfilerActionStateSetup() {
    override val icon: Icon = UnityIcons.Profiler.FetchInProgress
    override val tooltipTitle: String = UnityUIBundle.message("unity.profiler.integration.widget.fetchingInProgress.tooltipTitle")
    override val tooltipDescription: String = UnityUIBundle.message(
        "unity.profiler.integration.widget.fetchingInProgress.tooltipDescription",
        snapshotInfo.frameIndex,
        snapshotInfo.threadName
    )
}

/**
 * Represents the state when the profiling data is up to date with Unity's current state.
 * @property snapshotInfo Information about the current profiler snapshot, including frame and thread details
 */
class ProfilerActionStateDataIsUpToDate(
    private val snapshotInfo: UnityProfilerSnapshotStatus
) : ProfilerActionStateSetup() {
    override val icon: Icon = UnityIcons.Profiler.UpToDate
    override val tooltipTitle: String = UnityUIBundle.message("unity.profiler.integration.widget.dataIsUpToDate.tooltipTitle")
    override val tooltipDescription: String = UnityUIBundle.message(
        "unity.profiler.integration.widget.dataIsUpToDate.tooltipDescription",
        snapshotInfo.frameIndex,
        snapshotInfo.threadName
    )
}
