package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.backend.workspace.toVirtualFileUrl
import com.intellij.platform.backend.workspace.virtualFile
import com.intellij.platform.workspace.storage.EntityStorage
import com.intellij.workspaceModel.core.fileIndex.WorkspaceFileIndexContributor
import com.intellij.workspaceModel.core.fileIndex.WorkspaceFileKind
import com.intellij.workspaceModel.core.fileIndex.WorkspaceFileSetRegistrar
import com.intellij.workspaceModel.core.fileIndex.impl.ModuleOrLibrarySourceRootData
import com.intellij.workspaceModel.ide.impl.legacyBridge.module.findModule
import com.jetbrains.rider.model.RdSolutionDescriptor
import com.jetbrains.rider.projectView.indexing.RiderModuleRootData
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.workspaceModel.getRiderModuleEntity

class UnityWorkspaceFileIndexContributor : WorkspaceFileIndexContributor<ProjectModelEntity> {
    override val entityClass: Class<ProjectModelEntity>
        get() = ProjectModelEntity::class.java

    // Packages - almost works as expected
    // I have decided to manually add "Packages/manifest.json", "Packages/packages-lock.json", even though those are not in the generated projects

    // ProjectSettings, UserSettings
    // adding manually as EXTERNAL_SOURCE, search for it with Out-Of-Solution scope
    // files, which are in the project already, (like cs) should remain in the solution scope

    // Library - not sure, probably we don't need anything there, except packages, which are added differently

    // Assets, would be great to include, but would cause a great perf penalty, time for indexing

    override fun registerFileSets(entity: ProjectModelEntity, registrar: WorkspaceFileSetRegistrar, storage: EntityStorage) {
        if (entity.descriptor is RdSolutionDescriptor) {
            val url = entity.url ?: return
            val module = storage.getRiderModuleEntity()!!.findModule(storage)!!
            val virtualFileManager = WorkspaceModel.getInstance(module.project).getVirtualFileUrlManager()

            val solFolder = url.virtualFile?.parent?:return
            val packagesFolder = solFolder.findChild("Packages")
            if (packagesFolder != null){
                val manifestJson = packagesFolder.findChild("manifest.json")
                if (manifestJson != null)
                    registrar.registerNonRecursiveFileSet(manifestJson.toVirtualFileUrl(virtualFileManager), WorkspaceFileKind.CONTENT, entity,
                                                          RiderModuleRootData(module))
                val packagesLockJson = packagesFolder.findChild("packages-lock.json")
                if (packagesLockJson != null)
                    registrar.registerNonRecursiveFileSet(packagesLockJson.toVirtualFileUrl(virtualFileManager), WorkspaceFileKind.CONTENT, entity,
                                                          RiderModuleRootData(module))
            }

            val projectSettingsDir = solFolder.findChild("ProjectSettings")
            if (projectSettingsDir != null)
                registrar.registerFileSet(projectSettingsDir.toVirtualFileUrl(virtualFileManager), WorkspaceFileKind.EXTERNAL_SOURCE, entity,
                                                      UnityAssetsModulesFileSetData())

            val userSettingsDir = solFolder.findChild("UserSettings")
            if (userSettingsDir != null)
                registrar.registerFileSet(userSettingsDir.toVirtualFileUrl(virtualFileManager), WorkspaceFileKind.EXTERNAL_SOURCE, entity,
                                          UnityAssetsModulesFileSetData())

            // we can't include this,
            // I haven't checked myself, but on big projects this would cause a big perf penalty, delay in indexing
            //val assetsDir = solFolder.findChild("Assets")
            //if (assetsDir != null)
            //    registrar.registerFileSet(assetsDir.toVirtualFileUrl(virtualFileManager), WorkspaceFileKind.EXTERNAL_SOURCE, entity,
            //                              UnityAssetsModulesFileSetData())
        }
    }

    private class UnityAssetsModulesFileSetData : ModuleOrLibrarySourceRootData
}