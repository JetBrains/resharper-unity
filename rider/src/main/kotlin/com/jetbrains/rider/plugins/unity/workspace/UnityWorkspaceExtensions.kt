@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.workspace.jps.entities.ContentRootEntity
import com.intellij.platform.workspace.jps.entities.ExcludeUrlEntity
import com.intellij.platform.workspace.jps.entities.ModuleEntity
import com.intellij.platform.workspace.jps.entities.modifyModuleEntity
import com.intellij.platform.workspace.storage.EntitySource
import com.intellij.platform.workspace.storage.ExternalMappingKey
import com.intellij.platform.workspace.storage.MutableEntityStorage
import com.intellij.platform.workspace.storage.url.VirtualFileUrl

val UNITY_EXCLUDED_PATTERNS = listOf("*.tmp") // don't exclude meta - undo would not work RIDER-81449, see also RIDER-79712, RIDER-83846
val UNITY_PACKAGE_ID_MAPPING = ExternalMappingKey.create<String>("rider.unity.package.id")


fun WorkspaceModel.getPackages(): List<UnityPackageEntity> {
    return currentSnapshot.entities(UnityPackageEntity::class.java).toList()
}

fun WorkspaceModel.hasPackage(id: String): Boolean {
    val mapping = currentSnapshot.getExternalMapping(UNITY_PACKAGE_ID_MAPPING)
    return mapping.getEntities(id).any()
}

fun WorkspaceModel.tryGetPackage(id: String): UnityPackageEntity? {
    val mapping = currentSnapshot.getExternalMapping(UNITY_PACKAGE_ID_MAPPING)
    return mapping.getEntities(id).filterIsInstance<UnityPackageEntity>().singleOrNull()
}

fun WorkspaceModel.tryGetPackage(packageFolder: VirtualFile): UnityPackageEntity? {
    return getPackages().singleOrNull { it.packageFolder == packageFolder }
}

// previously was com.intellij.workspaceModel.storage.bridgeEntities.ExtensionsKt.addContentRootEntity
fun MutableEntityStorage.addContentRootEntity(url: VirtualFileUrl, //
                                              excludedUrls: List<VirtualFileUrl>,
                                              excludedPatterns: List<@NlsSafe String>,
                                              module: ModuleEntity,
                                              source: EntitySource = module.entitySource): ContentRootEntity {
    val excludes = excludedUrls.map { ExcludeUrlEntity(it, source) }
    val updatedModule = this.modifyModuleEntity(module) {
        this.contentRoots += ContentRootEntity(url, excludedPatterns, source) {
            this.excludedUrls = excludes
        }
    }
    return updatedModule.contentRoots.last()
}