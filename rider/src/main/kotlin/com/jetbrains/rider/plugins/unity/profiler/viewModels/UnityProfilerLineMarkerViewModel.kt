package com.jetbrains.rider.plugins.unity.profiler.viewModels

import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings

class UnityProfilerLineMarkerViewModel(val frontendBackendProfilerModel: FrontendBackendProfilerModel) {
    val gutterMarksRenderSettings: IOptProperty<ProfilerGutterMarkRenderSettings> get() = frontendBackendProfilerModel.gutterMarksRenderSettings
    fun navigateByQualifiedName(realParentQualifiedName: String) {
       frontendBackendProfilerModel.navigateByQualifiedName.fire(realParentQualifiedName) 
    }
}