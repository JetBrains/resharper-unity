package com.jetbrains.rider.plugins.unity.packageManager

import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.model.unity.frontendBackend.UnityPackage
import com.jetbrains.rider.model.unity.frontendBackend.UnityPackageDependency
import com.jetbrains.rider.model.unity.frontendBackend.UnityPackageSource
import java.nio.file.Paths

enum class PackageSource {
    Unknown,
    BuiltIn,
    Registry,
    Embedded,
    Local,
    LocalTarball,
    Git;

    fun isEditable(): Boolean {
        return this == Embedded || this == Local
    }

    fun isReadOnly(): Boolean {
        return !isEditable() && this != Unknown
    }
}

data class PackageData(val id: String,
                       val version: String,
                       val packageFolder: VirtualFile?,
                       val source: PackageSource,
                       val displayName: String,
                       val description: String?,
                       val dependencies: Map<String, String>,
                       val tarballLocation: String?,
                       val gitUrl: String?,
                       val gitHash: String?,
                       val gitRevision: String?) {
    companion object {
        fun fromUnityPackage(unityPackage: UnityPackage): PackageData {
            return PackageData(unityPackage.id,
                unityPackage.version,
                getPackagesFolder(unityPackage.packageFolderPath),
                toPackageSource(unityPackage.source),
                unityPackage.displayName,
                unityPackage.description,
                getDependencies(unityPackage.dependencies),
                unityPackage.tarballLocation,
                unityPackage.gitDetails?.url,
                unityPackage.gitDetails?.hash,
                unityPackage.gitDetails?.revision)
        }

        private fun getPackagesFolder(path: String?): VirtualFile? {
            if (path == null) return null
            return VfsUtil.findFile(Paths.get(path), true)
        }

        private fun toPackageSource(unityPackageSource: UnityPackageSource): PackageSource {
            return when (unityPackageSource) {
                UnityPackageSource.Unknown -> PackageSource.Unknown
                UnityPackageSource.BuiltIn -> PackageSource.BuiltIn
                UnityPackageSource.Registry -> PackageSource.Registry
                UnityPackageSource.Embedded -> PackageSource.Embedded
                UnityPackageSource.Local -> PackageSource.Local
                UnityPackageSource.LocalTarball -> PackageSource.LocalTarball
                UnityPackageSource.Git -> PackageSource.Git
            }
        }

        private fun getDependencies(dependencies: Array<UnityPackageDependency>) =
            dependencies.associate { it.id to it.version }
    }
}
