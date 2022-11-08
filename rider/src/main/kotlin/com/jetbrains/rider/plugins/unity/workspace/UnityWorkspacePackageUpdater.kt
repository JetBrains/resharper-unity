@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.util.application
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.intellij.workspaceModel.ide.getInstance
import com.intellij.workspaceModel.ide.impl.toVirtualFileUrl
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.bridgeEntities.addContentRootEntity
import com.intellij.workspaceModel.storage.url.VirtualFileUrlManager
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import com.jetbrains.rider.projectView.workspace.getOrCreateRiderModuleEntity
import java.nio.file.Paths

class UnityWorkspacePackageUpdater(private val project: Project) : LifetimedService() {

    companion object {
        private val logger = Logger.getInstance(UnityWorkspacePackageUpdater::class.java)
    }

    private var initialBuilder: MutableEntityStorage? = MutableEntityStorage.create()

    init {
        application.invokeLater {
            val model = project.solution.frontendBackendModel
            model.packages.adviseAddRemove(serviceLifetime) { action, _, unityPackage ->
                if (model.packagesUpdating.value != true) {
                    logger.error("Should not add/remove to packages without setting packagesUpdating first!")
                }
                when (action) {
                    AddRemove.Add -> updateWorkspaceModel { addPackage(unityPackage, it) }
                    AddRemove.Remove -> updateWorkspaceModel { removePackage(unityPackage, it) }
                }
            }
            model.packagesUpdating.advise(serviceLifetime) { updating ->
                // This property is NULL on startup, then true during constructing packages and then false
                //   when package list are already built
                if (updating == false) syncPackages()
            }
        }
    }

    private fun addPackage(unityPackage: UnityPackage, builder: MutableEntityStorage) {
        logger.trace("Adding Unity package: ${unityPackage.id}")

        val packageFolder = unityPackage.packageFolderPath?.let { VfsUtil.findFile(Paths.get(it), true) }
        val contentRootEntity = if (packageFolder != null && unityPackage.source != UnityPackageSource.Unknown) {
            builder.addContentRootEntity(
                packageFolder.toVirtualFileUrl(VirtualFileUrlManager.getInstance(project)),
                listOf(),
                UNITY_EXCLUDED_PATTERNS,
                builder.getOrCreateRiderModuleEntity(),
                RiderUnityPackageEntitySource)
        } else null

        val entity = UnityPackageEntity(unityPackage, RiderUnityPackageEntitySource) {
            this.descriptor = unityPackage
            this.contentRootEntity = contentRootEntity
        }
        builder.addEntity(entity)

        val mapping = builder.getMutableExternalMapping<String>(UNITY_PACKAGE_ID_MAPPING)
        mapping.addMapping(entity, entity.packageId)
    }

    private fun removePackage(unityPackage: UnityPackage, builder: MutableEntityStorage) {
        logger.trace("Removing Unity package: ${unityPackage.id}")

        val mapping = builder.getExternalMapping<String>(UNITY_PACKAGE_ID_MAPPING)
        for (entity in mapping.getEntities(unityPackage.id)) {
            builder.removeEntity(entity)
        }
    }

    private fun updateWorkspaceModel(action: (MutableEntityStorage) -> Unit) {
        application.runWriteAction {
            val initialBuilder = initialBuilder
            if (initialBuilder != null) {
                action.invoke(initialBuilder)
            } else {
                WorkspaceModel.getInstance(project).updateProjectModel(action)
            }
        }
    }

    private fun syncPackages() {
        val initialBuilder = initialBuilder ?: return
        this.initialBuilder = null
        logger.trace("Sync Unity packages after startup...")

        application.runWriteAction {
            WorkspaceModel.getInstance(project).updateProjectModel { x ->
                x.replaceBySource({ it is RiderUnityPackageEntitySource }, initialBuilder)
            }
        }
    }

    object RiderUnityPackageEntitySource : RiderEntitySource
}