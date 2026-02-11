package com.jetbrains.rider.plugins.unity.profiler

import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.IOptProperty
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FetchingMode
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings

class UnityProfilerIntegrationSettingsModel(val frontendBackendProfilerModel: FrontendBackendProfilerModel, lifetime: Lifetime) {
    val isIntegrationEnabled: IOptProperty<Boolean> get() = frontendBackendProfilerModel.isIntegraionEnable
    val fetchingMode: IOptProperty<FetchingMode> get() = frontendBackendProfilerModel.fetchingMode
    val gutterMarksRenderSettings: IOptProperty<ProfilerGutterMarkRenderSettings> get() = frontendBackendProfilerModel.gutterMarksRenderSettings
}