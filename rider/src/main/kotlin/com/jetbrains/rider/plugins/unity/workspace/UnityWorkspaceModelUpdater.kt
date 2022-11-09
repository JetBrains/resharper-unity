@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.project.Project
import com.intellij.util.application
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.intellij.workspaceModel.ide.getInstance
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.bridgeEntities.addContentRootEntity
import com.intellij.workspaceModel.storage.impl.url.toVirtualFileUrl
import com.intellij.workspaceModel.storage.url.VirtualFileUrl
import com.intellij.workspaceModel.storage.url.VirtualFileUrlManager
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import com.jetbrains.rider.projectView.workspace.getOrCreateRiderModuleEntity

class UnityWorkspaceModelUpdater(private val project: Project) {
    init {
        application.invokeLater {
            if (project.isDisposed)
                return@invokeLater

            if (project.isUnityProject()) {
                rebuildWorkspaceModel()
            }
        }
    }

    @Suppress("UnstableApiUsage")
    private fun rebuildWorkspaceModel() {
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