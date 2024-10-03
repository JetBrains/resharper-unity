@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.workspace.jps.entities.ContentRootEntity
import com.intellij.platform.workspace.storage.*
import com.intellij.platform.workspace.storage.annotations.Child
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource


interface UnityPackageEntity : WorkspaceEntity {

    val descriptor: UnityPackage

    val packageId: String get() = descriptor.id
    val version: String get() = descriptor.version
    val source: UnityPackageSource get() = descriptor.source
    val displayName: String get() = descriptor.displayName
    val description: String? get() = descriptor.description
    val dependencies: Map<String, String> get() = descriptor.dependencies.associate { it.id to it.version }
    val tarballLocation: String? get() = descriptor.tarballLocation
    val gitUrl: String? get() = descriptor.gitDetails?.url
    val gitHash: String? get() = descriptor.gitDetails?.hash
    val gitRevision: String? get() = descriptor.gitDetails?.revision

    fun isEditable(): Boolean {
        return descriptor.source in arrayOf(UnityPackageSource.Embedded, UnityPackageSource.Local)
    }

    fun isReadOnly(): Boolean {
        return !isEditable() && descriptor.source != UnityPackageSource.Unknown
    }

    val packageFolder: VirtualFile? get() = descriptor.packageFolderPath?.toVirtualFile(false)

    //region generated code
    @GeneratedCodeApiVersion(3)
    interface Builder : WorkspaceEntity.Builder<UnityPackageEntity> {
        override var entitySource: EntitySource
        var descriptor: UnityPackage
        var contentRootEntity: ContentRootEntity.Builder?
    }

    companion object : EntityType<UnityPackageEntity, Builder>() {
        @JvmOverloads
        @JvmStatic
        @JvmName("create")
        operator fun invoke(
            descriptor: UnityPackage,
            entitySource: EntitySource,
            init: (Builder.() -> Unit)? = null,
        ): Builder {
            val builder = builder()
            builder.descriptor = descriptor
            builder.entitySource = entitySource
            init?.invoke(builder)
            return builder
        }
    }
    //endregion
}

//region generated code
fun MutableEntityStorage.modifyUnityPackageEntity(
    entity: UnityPackageEntity,
    modification: UnityPackageEntity.Builder.() -> Unit,
): UnityPackageEntity {
    return modifyEntity(UnityPackageEntity.Builder::class.java, entity, modification)
}

var ContentRootEntity.Builder.unityPackageEntity: UnityPackageEntity.Builder?
    by WorkspaceEntity.extensionBuilder(UnityPackageEntity::class.java)
//endregion

@Suppress("unused")
internal val ContentRootEntity.unityPackageEntity: UnityPackageEntity? by WorkspaceEntity.extension()