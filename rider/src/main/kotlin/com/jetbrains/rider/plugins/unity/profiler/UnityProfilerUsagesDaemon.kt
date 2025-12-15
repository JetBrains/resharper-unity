package com.jetbrains.rider.plugins.unity.profiler

import com.intellij.openapi.components.Service
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendProfilerModel
import com.jetbrains.rider.projectView.solution
import java.util.concurrent.atomic.AtomicLong

@Service(Service.Level.PROJECT)
class UnityProfilerUsagesDaemon(project: Project) {

    private val frontendBackendProfilerModel: FrontendBackendProfilerModel? =
        when {
            project.isUnityProject.value -> {
                project.solution.frontendBackendModel.frontendBackendProfilerModel
            }
            else -> {
                null
            }
        }  

    fun showPopupAction() {
       frontendBackendProfilerModel?.showPopupAction?.fire(Unit) 
    }
}
