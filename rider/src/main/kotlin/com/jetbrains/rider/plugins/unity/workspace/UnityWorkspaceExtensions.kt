@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.WorkspaceModel
import com.intellij.workspaceModel.storage.EntitySource
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.bridgeEntities.ContentRootEntity
import com.intellij.workspaceModel.storage.bridgeEntities.ExcludeUrlEntity
import com.intellij.workspaceModel.storage.bridgeEntities.ModuleEntity
import com.intellij.workspaceModel.storage.url.VirtualFileUrl

val UNITY_EXCLUDED_PATTERNS = listOf("*.tmp") // don't exclude meta - undo would not work RIDER-81449, see also RIDER-79712, RIDER-83846
val UNITY_PACKAGE_ID_MAPPING = "rider.unity.package.id"


fun WorkspaceModel.getPackages(): List<UnityPackageEntity> {
    return entityStorage.current.entities(UnityPackageEntity::class.java).toList()
}

fun WorkspaceModel.hasPackage(id: String): Boolean {
    val mapping = entityStorage.current.getExternalMapping<String>(UNITY_PACKAGE_ID_MAPPING)
    return mapping.getEntities(id).any()
}

fun WorkspaceModel.tryGetPackage(id: String): UnityPackageEntity? {
    val mapping = entityStorage.current.getExternalMapping<String>(UNITY_PACKAGE_ID_MAPPING)
    return mapping.getEntities(id).filterIsInstance<UnityPackageEntity>().singleOrNull()
}

fun WorkspaceModel.tryGetPackage(packageFolder: VirtualFile): UnityPackageEntity? {
    return getPackages().singleOrNull { it.packageFolder == packageFolder }
}

// previously was com.intellij.workspaceModel.storage.bridgeEntities.ExtensionsKt.addContentRootEntity
fun MutableEntityStorage.addContentRootEntity(url: VirtualFileUrl,
                                              excludedUrls: List<VirtualFileUrl>,
                                              excludedPatterns: List<@NlsSafe String>,
                                              module: ModuleEntity,
                                              source: EntitySource = module.entitySource): ContentRootEntity {
    val excludes = excludedUrls.map { this addEntity ExcludeUrlEntity(it, source) }
    return this addEntity ContentRootEntity(url, excludedPatterns, source) {
        this.excludedUrls = excludes
        this.module = module
    }
}