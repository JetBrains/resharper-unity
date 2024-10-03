package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.backend.workspace.toVirtualFileUrl
import com.intellij.platform.workspace.storage.EntityStorage
import com.intellij.workspaceModel.core.fileIndex.WorkspaceFileIndexContributor
import com.intellij.workspaceModel.core.fileIndex.WorkspaceFileKind
import com.intellij.workspaceModel.core.fileIndex.WorkspaceFileSetRegistrar
import com.intellij.workspaceModel.core.fileIndex.impl.ModuleOrLibrarySourceRootData
import com.intellij.workspaceModel.ide.impl.legacyBridge.module.findModule
import com.jetbrains.rider.workspaceModel.getRiderModuleEntity

class UnityPackagesWorkspaceFileIndexContributor: WorkspaceFileIndexContributor<UnityPackageEntity> {
    override val entityClass: Class<UnityPackageEntity>
        get() = UnityPackageEntity::class.java

    // RIDER-116592 Searching in Unity is filled with clutter.
    // we need to somehow avoid adding all files in the registry package to the Solution scope

    // RIDER-65779 Find in files looks into assemblies items with `Include non-solution items` option disabled
    // we need to look in the registry packages, only when `Include non-solution items` is specified

    override fun registerFileSets(entity: UnityPackageEntity, registrar: WorkspaceFileSetRegistrar, storage: EntityStorage) {
        val module = storage.getRiderModuleEntity()!!.findModule(storage)!!
        val virtualFileManager = WorkspaceModel.getInstance(module.project).getVirtualFileUrlManager()
        val url = entity.packageFolder?.toVirtualFileUrl(virtualFileManager)
        if (url == null ) return

        registrar.registerFileSet(url, WorkspaceFileKind.EXTERNAL_SOURCE, entity, UnityModulesFileSetData())
    }
    private class UnityModulesFileSetData : ModuleOrLibrarySourceRootData
}