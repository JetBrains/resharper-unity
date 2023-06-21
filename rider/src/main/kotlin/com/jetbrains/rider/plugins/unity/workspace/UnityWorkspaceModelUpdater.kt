@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.withUiContext
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.util.application
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.intellij.workspaceModel.ide.getInstance
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.impl.url.toVirtualFileUrl
import com.intellij.workspaceModel.storage.url.VirtualFileUrl
import com.intellij.workspaceModel.storage.url.VirtualFileUrlManager
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import com.jetbrains.rider.projectView.workspace.getOrCreateRiderModuleEntity

class UnityWorkspaceModelUpdaterInitializer : ProjectActivity {
    override suspend fun execute(project: Project) {
        if (!project.isDisposed && project.isUnityProject()) {
            withUiContext { UnityWorkspaceModelUpdater.getInstance(project).rebuildModel() }
        }
    }
}

@Service(Service.Level.PROJECT)
class UnityWorkspaceModelUpdater(private val project: Project) {
    companion object {
        fun getInstance(project: Project):UnityWorkspaceModelUpdater =  project.service()
    }

    @Suppress("UnstableApiUsage")
    fun rebuildModel() {
        application.assertIsDispatchThread()
        if (!project.isUnityProject()) return

        val builder = MutableEntityStorage.create()

        val virtualFileUrlManager = VirtualFileUrlManager.getInstance(project)
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
            WorkspaceModel.getInstance(project).updateProjectModel("Unity: update workspace model") {
                x -> x.replaceBySource({ it is RiderUnityEntitySource }, builder)
            }
        }
    }

    object RiderUnityEntitySource : RiderEntitySource
}