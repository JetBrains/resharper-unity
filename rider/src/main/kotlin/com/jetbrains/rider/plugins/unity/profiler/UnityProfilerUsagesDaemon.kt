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

@Service(Service.Level.PROJECT)
class UnityProfilerUsagesDaemon(project: Project) : Disposable {

    private val frontendBackendModel: FrontendBackendModel = project.solution.frontendBackendModel
    private val frontendBackendProfilerModel: FrontendBackendProfilerModel = frontendBackendModel.frontendBackendProfilerModel

    val isUnityProject: Boolean = project.isUnityProject.value
    val lifetime: Lifetime = UnityProjectLifetimeService.getLifetime(project)

    //view models
    val settingsModel: UnityProfilerIntegrationSettingsModel = UnityProfilerIntegrationSettingsModel(frontendBackendProfilerModel, lifetime)
    val snapshotModel: UnityProfilerSnapshotModel = UnityProfilerSnapshotModel(frontendBackendProfilerModel, lifetime)
    val chartViewModel: UnityProfilerChartViewModel = UnityProfilerChartViewModel(frontendBackendProfilerModel, lifetime)
    val treeViewModel: UnityProfilerTreeViewModel = UnityProfilerTreeViewModel(frontendBackendProfilerModel, snapshotModel, lifetime)
    val lineMarkerViewModel: UnityProfilerLineMarkerViewModel = UnityProfilerLineMarkerViewModel(frontendBackendProfilerModel)

    override fun dispose() {
        // ViewModels and RD models are disposed via the lifetime
        // Explicit cleanup can be added here if needed
    }
}

