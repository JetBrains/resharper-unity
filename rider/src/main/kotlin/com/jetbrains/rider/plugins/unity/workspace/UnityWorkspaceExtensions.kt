@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.WorkspaceModel

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