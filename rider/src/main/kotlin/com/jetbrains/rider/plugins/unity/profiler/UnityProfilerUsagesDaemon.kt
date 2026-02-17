package com.jetbrains.rider.plugins.unity.profiler

import com.intellij.openapi.Disposable
import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerChartViewModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerLineMarkerViewModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerSnapshotModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerTreeViewModel
import com.jetbrains.rider.projectView.solution
import java.util.concurrent.atomic.AtomicInteger

@Service(Service.Level.PROJECT)
class UnityProfilerUsagesDaemon(private val project: Project) : Disposable {

    private val frontendBackendModel: FrontendBackendModel = project.solution.frontendBackendModel
    private val frontendBackendProfilerModel: FrontendBackendProfilerModel = frontendBackendModel.frontendBackendProfilerModel

    val isUnityProject: Boolean = project.isUnityProject.value
    val lifetime: Lifetime = UnityProjectLifetimeService.getLifetime(project)

    // Per-project session counters for telemetry (thread-safe)
    private val gutterClickCount = AtomicInteger(0)
    private val graphClickCount = AtomicInteger(0)
    private val treeInteractionCount = AtomicInteger(0)

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

        // Flush usage statistics when lifetime terminates (project closes or daemon is disposed)
        lifetime.onTermination {
            flushTelemetry()
        }
    }

    // Telemetry API - increment session counters
    fun incrementGutterClick() {
        gutterClickCount.incrementAndGet()
    }

    fun incrementGraphClick() {
        graphClickCount.incrementAndGet()
    }

    fun incrementTreeInteraction() {
        treeInteractionCount.incrementAndGet()
    }

    private fun flushTelemetry() {
        val gutterClicks = gutterClickCount.getAndSet(0)
        val graphClicks = graphClickCount.getAndSet(0)
        val treeInteractions = treeInteractionCount.getAndSet(0)

        UnityProfilerUsagesCollector.logSessionInteractions(project, gutterClicks, graphClicks, treeInteractions)
    }

    override fun dispose() {
        // ViewModels and RD models are disposed via the lifetime
        // Note: lifetime.onTermination already handles flushing statistics
    }
}

