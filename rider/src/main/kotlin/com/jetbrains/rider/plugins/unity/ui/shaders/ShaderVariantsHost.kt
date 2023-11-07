package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetManager
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ShaderVariantsHost : ProjectActivity {
    override suspend fun execute(project: Project) {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        val frontendBackendHost = FrontendBackendHost.getInstance(project)
        val model = frontendBackendHost.model
        withContext(Dispatchers.EDT) {
            model.backendSettings.previewShaderVariantsSupport.advise(lifetime) {
                RiderResolveContextWidgetManager.invalidateWidgets(project)
            }
        }
    }
}