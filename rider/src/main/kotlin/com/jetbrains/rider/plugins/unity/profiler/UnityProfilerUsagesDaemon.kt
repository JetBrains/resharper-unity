package com.jetbrains.rider.plugins.unity.profiler

import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.profiler.toolWindow.UnityProfilerToolWindowFactory
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerChartViewModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerLineMarkerViewModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerSnapshotModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerTreeViewModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.util.concurrent.atomic.AtomicInteger

@Service(Service.Level.PROJECT)
class UnityProfilerUsagesDaemon(private val project: Project) {

    companion object {
        private const val TOOL_WINDOW_AUTO_SHOWN_KEY = "unity.profiler.toolWindow.autoShown"
        private const val EVENTS_COUNT_BEFORE_FLASH_COUNTERS = 64
    }


    private val frontendBackendModel: FrontendBackendModel = project.solution.frontendBackendModel
    private val frontendBackendProfilerModel: FrontendBackendProfilerModel = frontendBackendModel.frontendBackendProfilerModel

    val isUnityProject: Boolean = project.isUnityProject.value
    val lifetime: Lifetime = UnityProjectLifetimeService.getLifetime(project)

    // Per-project session counters for telemetry (thread-safe)
    private val gutterClickCount = AtomicInteger(0)
    private val graphClickCount = AtomicInteger(0)
    private val treeInteractionCount = AtomicInteger(0)
    
    val unityEditorConnected: IOptProperty<Boolean> get() = frontendBackendModel.unityEditorConnected

    //view models
    val settingsModel: UnityProfilerIntegrationSettingsModel = UnityProfilerIntegrationSettingsModel(frontendBackendProfilerModel, lifetime)
    val snapshotModel: UnityProfilerSnapshotModel = UnityProfilerSnapshotModel(frontendBackendProfilerModel, lifetime)
    val chartViewModel: UnityProfilerChartViewModel = UnityProfilerChartViewModel(frontendBackendProfilerModel, lifetime)
    val treeViewModel: UnityProfilerTreeViewModel = UnityProfilerTreeViewModel(frontendBackendProfilerModel, snapshotModel, project, lifetime)
    val lineMarkerViewModel: UnityProfilerLineMarkerViewModel = UnityProfilerLineMarkerViewModel(frontendBackendProfilerModel, project)

    init {
        // Subscribe to backend telemetry events
        frontendBackendProfilerModel.logSelectedFrameInUnityProfiler.advise(lifetime) { frameIndex ->
            UnityProfilerUsagesCollector.logSelectedFrameInUnityProfiler(project, frameIndex)
        }

        frontendBackendProfilerModel.logNavigatedFromUnityProfiler.advise(lifetime) {
            UnityProfilerUsagesCollector.logNavigatedFromUnityProfiler(project)
        }

        frontendBackendProfilerModel.logSnapshotFetched.advise(lifetime) { args ->
            UnityProfilerUsagesCollector.logSnapshotFetched(project, args.frameIndex, args.duration)
        }

        // Auto-open the tool window once when first valid snapshot is received
        frontendBackendProfilerModel.currentSnapshot.advise(lifetime) { snapshot ->
            if (snapshot != null && snapshot.samples.isNotEmpty()) {
                tryAutoOpenToolWindowOnce()
            }
        }

        // Flush usage statistics when lifetime terminates (project closes or daemon is disposed)
        lifetime.onTermination {
            flushTelemetry()
        }
    }

    // Telemetry API - increment session counters
    fun incrementGutterClick() {
        if (gutterClickCount.incrementAndGet() >= EVENTS_COUNT_BEFORE_FLASH_COUNTERS) {
            flushTelemetry()
        }
    }

    fun incrementGraphClick() {
        if (graphClickCount.incrementAndGet() >= EVENTS_COUNT_BEFORE_FLASH_COUNTERS) {
            flushTelemetry()
        }
    }

    fun incrementTreeInteraction() {
        if (treeInteractionCount.incrementAndGet() >= EVENTS_COUNT_BEFORE_FLASH_COUNTERS) {
            flushTelemetry()
        }
    }

    private fun flushTelemetry() {
        val gutterClicks = gutterClickCount.getAndSet(0)
        val graphClicks = graphClickCount.getAndSet(0)
        val treeInteractions = treeInteractionCount.getAndSet(0)

        UnityProfilerUsagesCollector.logSessionInteractions(project, gutterClicks, graphClicks, treeInteractions)
    }

    /**
     * Auto-opens the Unity Profiler tool window once per project when valid profiling data is available.
     * This promotes discoverability of the feature without being intrusive.
     */
    private fun tryAutoOpenToolWindowOnce() {
        val properties = PropertiesComponent.getInstance(project)
        if (properties.getBoolean(TOOL_WINDOW_AUTO_SHOWN_KEY, false)) {
            return
        }

        properties.setValue(TOOL_WINDOW_AUTO_SHOWN_KEY, true)

        lifetime.coroutineScope.launch(Dispatchers.EDT) {
            UnityProfilerToolWindowFactory.show(project)
        }
    }
}

