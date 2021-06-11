@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.workspaceModel.ide.getInstance
import com.intellij.workspaceModel.ide.impl.toVirtualFileUrl
import com.intellij.workspaceModel.storage.WorkspaceEntityStorageBuilder
import com.intellij.workspaceModel.storage.bridgeEntities.addContentRootEntityWithCustomEntitySource
import com.intellij.workspaceModel.storage.url.VirtualFileUrlManager
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.platform.util.idea.LifetimedProjectService
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rider.model.unity.frontendBackend.UnityPackage
import com.jetbrains.rider.model.unity.frontendBackend.UnityPackageSource
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import com.jetbrains.rider.projectView.workspace.getOrCreateRiderModuleEntity
import com.jetbrains.rider.projectView.workspace.impl.WorkspaceModelEditingFacade
import java.nio.file.Paths

class UnityWorkspacePackageUpdater(project: Project) : LifetimedProjectService(project) {

    companion object {
        private val logger = Logger.getInstance(UnityWorkspacePackageUpdater::class.java)
    }

    private var initialBuilder: WorkspaceEntityStorageBuilder? = WorkspaceEntityStorageBuilder.create()

    init {
        application.invokeLater {
            val model = project.solution.frontendBackendModel
            model.packages.adviseAddRemove(projectServiceLifetime) { action, _, unityPackage ->
                if (model.packagesUpdating.value != true) {
                    logger.error("Should not add/remove to packages without setting packagesUpdating first!")
                }
                when (action) {
                    AddRemove.Add -> updateWorkspaceModel { addPackage(unityPackage, it) }
                    AddRemove.Remove -> updateWorkspaceModel { removePackage(unityPackage, it) }
                }
            }
            model.packagesUpdating.advise(projectServiceLifetime) { updating ->
                // This property is NULL on startup, then true during constructing packages and then false
                //   when package list are already built
                if (updating == false) syncPackages()
            }
        }
    }

    private fun addPackage(unityPackage: UnityPackage, builder: WorkspaceEntityStorageBuilder) {
        logger.trace("Adding Unity package: ${unityPackage.id}")

        val packageFolder = unityPackage.packageFolderPath?.let { VfsUtil.findFile(Paths.get(it), true) }
        val contentRootEntity = if (packageFolder != null && unityPackage.source != UnityPackageSource.Unknown) {
            builder.addContentRootEntityWithCustomEntitySource(
                packageFolder.toVirtualFileUrl(VirtualFileUrlManager.getInstance(project)),
                listOf(),
                UNITY_EXCLUDED_PATTERNS,
                builder.getOrCreateRiderModuleEntity(),
                RiderUnityPackageEntitySource)
        } else null

        val entity = builder.addEntity(ModifiableUnityPackageEntity::class.java, RiderUnityPackageEntitySource) {
            this.descriptor = unityPackage
            this.contentRootEntity = contentRootEntity
        }

        val mapping = builder.getMutableExternalMapping<String>(UNITY_PACKAGE_ID_MAPPING)
        mapping.addMapping(entity, entity.id)
    }

    private fun removePackage(unityPackage: UnityPackage, builder: WorkspaceEntityStorageBuilder) {
        logger.trace("Removing Unity package: ${unityPackage.id}")

        val mapping = builder.getExternalMapping<String>(UNITY_PACKAGE_ID_MAPPING)
        for (entity in mapping.getEntities(unityPackage.id)) {
            builder.removeEntity(entity)
        }
    }

    private fun updateWorkspaceModel(action: (WorkspaceEntityStorageBuilder) -> Unit) {
        application.runWriteAction {
            val initialBuilder = initialBuilder
            if (initialBuilder != null) {
                action.invoke(initialBuilder)
            } else {
                val workspaceModel = WorkspaceModelEditingFacade.getInstance(project).getWorkspaceModelForEditing()
                workspaceModel.updateProjectModel(action)
            }
        }
    }

    private fun syncPackages() {
        val initialBuilder = initialBuilder ?: return
        this.initialBuilder = null
        logger.trace("Sync Unity packages after startup...")

        application.runWriteAction {
            val workspaceModel = WorkspaceModelEditingFacade.getInstance(project).getWorkspaceModelForEditing()
            workspaceModel.updateProjectModel { x -> x.replaceBySource({ it is RiderUnityPackageEntitySource }, initialBuilder) }
        }
    }

    object RiderUnityPackageEntitySource : RiderEntitySource
}