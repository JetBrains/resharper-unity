@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.application.EDT
import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vfs.VirtualFilePrefixTree
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.workspace.storage.MutableEntityStorage
import com.intellij.util.application
import com.intellij.util.concurrency.ThreadingAssertions
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rd.util.threading.coroutines.launch
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.RiderEntitySource
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.withContext
import java.nio.file.Paths

@Service(Service.Level.PROJECT)
class UnityWorkspacePackageUpdater(private val project: Project) {

    companion object {
        private val logger = Logger.getInstance(UnityWorkspacePackageUpdater::class.java)
        fun getInstance(project: Project): UnityWorkspacePackageUpdater = project.service()
    }

    private var initialEntityStorage: MutableEntityStorage? = MutableEntityStorage.create()
    val sourceRootsTree = VirtualFilePrefixTree.createSet()

    init {
        val assets = project.solutionDirectory.toVirtualFile(false)?.findChild("Assets")
        if (assets != null) sourceRootsTree.add(assets)
        else logger.warn("No `Assets` folder in the Unity project")
        // Very tiny chance, that UnityWorkspacePackageUpdater gets created on event of removing the Assets folder
        // RIDER-98395 Fix FileSystemExplorerActionsTest.testDeleteFolderInExplorer after adding Unity
    }

    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            // Subscribe to package changes. If we subscribe after the backend has loaded the initial list of packages,
            // this map will already be populated, and we'll be called for each item. If we subscribe before the list
            // is loaded, updateWorkspaceModel will cache the changes until the packagesUpdating flag is reset. At this
            // point, we sync the cached changes, and switch to updating the workspace model directly. We then expect
            // Unity to only add/remove a single package at a time.
            val updater = getInstance(session.project)
            model.packages.adviseAddRemove(lifetime) { action, _, unityPackage ->
                logger.trace("$action: ${unityPackage.id}")
                ThreadingAssertions.assertEventDispatchThread()
                // Enqueue event for ordered, non-blocking processing
                updater.eventsChannel.trySend(PackageEvent(action, unityPackage))
            }

            updater.start(lifetime)

            // Wait for the property to become false. On the backend, it is initially null, set to true before initially
            // calculating the packages, and then set to false. Depending on when we subscribe, we might not see all
            // states, but as long as it's false, we know it's done the initial bulk update and isn't working right now,
            // so we can flush the cached changes from the initial update.
            model.packagesUpdating.adviseUntil(lifetime) { updating ->
                if (updating == false) {
                    getInstance(session.project).syncInitialEntityStorage()
                    return@adviseUntil true
                }
                return@adviseUntil false
            }
        }
    }

    private data class PackageEvent(val action: AddRemove, val unityPackage: UnityPackage)
    private val eventsChannel = Channel<PackageEvent>(Channel.UNLIMITED)

    private fun start(lifetime: Lifetime) {
        val updater = this
        // Process events sequentially on IO dispatcher; switch to EDT for model updates
        lifetime.launch (Dispatchers.IO) {
            for (event in eventsChannel) { // equivalent to a “consume forever” while loop
                val unityPackage = event.unityPackage
                val packageFolder = unityPackage.packageFolderPath?.let { path ->
                    VfsUtil.findFile(Paths.get(path), true)
                }
                withContext(Dispatchers.EDT) {
                    when (event.action) {
                        AddRemove.Add -> updater.updateWorkspaceModel { entityStorage ->
                            if (packageFolder != null) {
                                logger.trace("Adding to the sourceRootsTree: ${unityPackage.packageFolderPath}")
                                updater.sourceRootsTree.add(packageFolder)
                            }
                            updater.addPackage(unityPackage, packageFolder, entityStorage)
                        }
                        AddRemove.Remove -> updater.updateWorkspaceModel { entityStorage ->
                            if (packageFolder != null) {
                                logger.trace("Removing from the sourceRootsTree: ${unityPackage.packageFolderPath}")
                                updater.sourceRootsTree.remove(packageFolder)
                            }
                            updater.removePackage(unityPackage, entityStorage)
                        }
                    }
                }
            }
        }
    }

    private fun addPackage(unityPackage: UnityPackage, packageFolder: VirtualFile?, entityStorage: MutableEntityStorage) {
        logger.trace("Adding Unity package: ${unityPackage.id}")

        if (packageFolder != null && unityPackage.source != UnityPackageSource.Unknown){
            val entity = entityStorage.addEntity(UnityPackageEntity(unityPackage, RiderUnityPackageEntitySource) {
                this.descriptor = unityPackage
            })
            val mapping = entityStorage.getMutableExternalMapping(UNITY_PACKAGE_ID_MAPPING)
            mapping.addMapping(entity, unityPackage.id)
        }
    }

    private fun removePackage(unityPackage: UnityPackage, entityStorage: MutableEntityStorage) {
        logger.trace("Removing Unity package: ${unityPackage.id}")

        val mapping = entityStorage.getExternalMapping(UNITY_PACKAGE_ID_MAPPING)
        for (entity in mapping.getEntities(unityPackage.id)) {
            entityStorage.removeEntity(entity)
        }
    }

    private fun updateWorkspaceModel(action: (MutableEntityStorage) -> Unit) {
        application.runWriteAction {
            val entityStorage = initialEntityStorage
            if (entityStorage != null) {
                action.invoke(entityStorage)
            }
            else {
                WorkspaceModel.getInstance(project).updateProjectModel("Unity: update packages", action)
            }
        }
    }

    private fun syncInitialEntityStorage() {
        val initialEntityStorage = initialEntityStorage ?: return
        logger.trace("Sync Unity packages after startup...")

        application.runWriteAction {
            this.initialEntityStorage = null
            WorkspaceModel.getInstance(project).updateProjectModel("Unity: sync packages") { entityStorage ->
                entityStorage.replaceBySource({ it is RiderUnityPackageEntitySource }, initialEntityStorage)
            }
        }
    }

    object RiderUnityPackageEntitySource : RiderEntitySource
}