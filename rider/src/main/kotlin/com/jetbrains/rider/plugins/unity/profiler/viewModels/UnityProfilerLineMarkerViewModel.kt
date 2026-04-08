package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerNavigationRequest
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesCollector

class UnityProfilerLineMarkerViewModel(
    val frontendBackendProfilerModel: FrontendBackendProfilerModel,
    private val project: Project
) {
    val gutterMarksRenderSettings: IOptProperty<ProfilerGutterMarkRenderSettings> get() = frontendBackendProfilerModel.gutterMarksRenderSettings
    val isGutterMarksEnabled: IOptProperty<Boolean> get() = frontendBackendProfilerModel.isGutterMarksEnabled

    fun navigateByQualifiedName(realParentQualifiedName: String) {
        frontendBackendProfilerModel.navigateByQualifiedName.fire(ProfilerNavigationRequest(realParentQualifiedName, null))
        UnityProfilerUsagesCollector.logNavigateGutterToParentCall(project)
    }
}