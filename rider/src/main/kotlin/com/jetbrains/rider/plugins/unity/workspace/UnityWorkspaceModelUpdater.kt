@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.startOnUiAsync
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.workspace.storage.MutableEntityStorage
import com.intellij.platform.workspace.storage.impl.url.toVirtualFileUrl
import com.intellij.platform.workspace.storage.url.VirtualFileUrl
import com.intellij.util.application
import com.intellij.util.concurrency.annotations.RequiresEdt
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import com.jetbrains.rider.workspaceModel.getOrCreateRiderModuleEntity

class UnityWorkspaceModelUpdaterInitializer : ProjectActivity {
    override suspend fun execute(project: Project) {

        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        UnityProjectDiscoverer.getInstance(project).isUnityProject.adviseUntil(lifetime) {
            if (it) {
                lifetime.startOnUiAsync {
                    UnityWorkspaceModelUpdater.getInstance(project).rebuildModel()
                }
            }
            it
        }
    }
}

@Service(Service.Level.PROJECT)
class UnityWorkspaceModelUpdater(private val project: Project) {
    companion object {
        fun getInstance(project: Project): UnityWorkspaceModelUpdater = project.service()
    }

    @RequiresEdt
    @Suppress("UnstableApiUsage")
    fun rebuildModel() {
        if (!project.isUnityProject.value) return

        val builder = MutableEntityStorage.create()

        val workspaceModel = WorkspaceModel.getInstance(project)
        val virtualFileUrlManager = workspaceModel.getVirtualFileUrlManager()
        val packagesModuleEntity = builder.getOrCreateRiderModuleEntity()

        // TODO: WORKSPACEMODEL
        // We want to include list of special files (by extensions comes from unity editor)
        // in the content model. It is better to do it on backed via backend PackageManager

        val excludedUrls = emptyList<VirtualFileUrl>()
        val excludedPatterns = UNITY_EXCLUDED_PATTERNS

        builder.addContentRootEntity(
            project.solutionDirectory.resolve("Packages").toVirtualFileUrl(virtualFileUrlManager),
            excludedUrls,
            excludedPatterns,
            packagesModuleEntity,
            RiderUnityEntitySource)

        builder.addContentRootEntity(
            project.solutionDirectory.resolve("ProjectSettings").toVirtualFileUrl(virtualFileUrlManager),
            excludedUrls,
            excludedPatterns,
            packagesModuleEntity,
            RiderUnityEntitySource)

        application.runWriteAction {
            workspaceModel.updateProjectModel("Unity: update workspace model") { x ->
                x.replaceBySource({ it is RiderUnityEntitySource }, builder)
            }
        }
    }

    object RiderUnityEntitySource : RiderEntitySource
}