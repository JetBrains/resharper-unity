package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.project.Project
import com.intellij.workspaceModel.ide.getInstance
import com.intellij.workspaceModel.ide.impl.toVirtualFileUrl
import com.intellij.workspaceModel.storage.WorkspaceEntityStorageBuilder
import com.intellij.workspaceModel.storage.bridgeEntities.addContentRootEntityWithCustomEntitySource
import com.intellij.workspaceModel.storage.impl.url.toVirtualFileUrl
import com.intellij.workspaceModel.storage.url.VirtualFileUrlManager
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rider.plugins.unity.packageManager.PackageManager
import com.jetbrains.rider.plugins.unity.packageManager.PackageManagerListener
import com.jetbrains.rider.plugins.unity.packageManager.PackageSource
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import com.jetbrains.rider.projectView.workspace.getOrCreateRiderModuleEntity
import com.jetbrains.rider.projectView.workspace.impl.WorkspaceModelEditingFacade

class UnityWorkspaceModelUpdater(private val project: Project) {
    init {
        application.invokeLater {
            rebuildWorkspaceModel()

            // Listen for external packages that we should index. Called on the UI thread
            PackageManager.getInstance(project).addListener(object : PackageManagerListener {
                override fun onPackagesUpdated() {
                    rebuildWorkspaceModel()
                }
            })
        }
    }

    @Suppress("UnstableApiUsage")
    private fun rebuildWorkspaceModel() {
        application.assertIsDispatchThread()

        val builder = WorkspaceEntityStorageBuilder.create()
        val virtualFileUrlManager = VirtualFileUrlManager.getInstance(project)

        val packagesModuleEntity = builder.getOrCreateRiderModuleEntity()

        // TODO: WORKSPACEMODEL
        // We want to include list of special files (by extensions comes from unity editor)
        // in the content model. It is better to do it on backed via backend PackageManager

        builder.addContentRootEntityWithCustomEntitySource(
            project.solutionDirectory.resolve("Packages").toVirtualFileUrl(virtualFileUrlManager), listOf(), listOf("*.meta", "*.tmp"), packagesModuleEntity
        , RiderUnityEntitySource)
        builder.addContentRootEntityWithCustomEntitySource(
            project.solutionDirectory.resolve("ProjectSettings").toVirtualFileUrl(virtualFileUrlManager), listOf(), listOf("*.meta", "*.tmp"), packagesModuleEntity
            , RiderUnityEntitySource)

        val packages = PackageManager.getInstance(project).getPackages()
        if (packages.any()) {
            for (packageData in packages) {
                val packageFolder = packageData.packageFolder ?: continue
                if (packageData.source !in arrayOf(PackageSource.Embedded, PackageSource.Unknown)) {
                    builder.addContentRootEntityWithCustomEntitySource(
                        packageFolder.toVirtualFileUrl(virtualFileUrlManager), listOf(), listOf("*.meta", "*.tmp"), packagesModuleEntity
                    , RiderUnityEntitySource)
                }
            }
        }

        application.runWriteAction {
            val projectModel = WorkspaceModelEditingFacade.getInstance(project).getWorkspaceModelForEditing()
            projectModel.updateProjectModel { x -> x.replaceBySource({ it is RiderUnityEntitySource }, builder) }
        }
    }

    object RiderUnityEntitySource : RiderEntitySource
}