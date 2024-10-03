package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.platform.workspace.storage.ExternalMappingKey

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
