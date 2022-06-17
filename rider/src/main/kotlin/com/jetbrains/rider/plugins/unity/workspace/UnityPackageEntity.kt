@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.deft.api.annotations.Ignore
import com.intellij.workspaceModel.ide.impl.virtualFile
import com.intellij.workspaceModel.storage.*
import com.intellij.workspaceModel.storage.bridgeEntities.api.ContentRootEntity
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource
import org.jetbrains.deft.ObjBuilder
import org.jetbrains.deft.Type
import com.intellij.workspaceModel.storage.EntitySource
import com.intellij.workspaceModel.storage.GeneratedCodeApiVersion
import com.intellij.workspaceModel.storage.ModifiableWorkspaceEntity
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.referrersx
import com.intellij.workspaceModel.storage.ModifiableReferableWorkspaceEntity
import com.intellij.workspaceModel.storage.WorkspaceEntity


interface UnityPackageEntity : WorkspaceEntity {

    val descriptor: UnityPackage

    @Ignore val packageId: String get() = descriptor.id
    @Ignore val version: String get() = descriptor.version
    @Ignore val source: UnityPackageSource get() = descriptor.source
    @Ignore val displayName: String get() = descriptor.displayName
    @Ignore val description: String? get() = descriptor.description
    @Ignore val dependencies: Map<String, String> get() = descriptor.dependencies.associate { it.id to it.version }
    @Ignore val tarballLocation: String? get() = descriptor.tarballLocation
    @Ignore val gitUrl: String? get() = descriptor.gitDetails?.url
    @Ignore val gitHash: String? get() = descriptor.gitDetails?.hash
    @Ignore val gitRevision: String?  get() = descriptor.gitDetails?.revision

    fun isEditable(): Boolean {
        return descriptor.source in arrayOf(UnityPackageSource.Embedded, UnityPackageSource.Local)
    }

    fun isReadOnly(): Boolean {
        return !isEditable() && descriptor.source != UnityPackageSource.Unknown
    }

    val contentRootEntity: ContentRootEntity?
    @Ignore val packageFolder: VirtualFile? get() = contentRootEntity?.url?.virtualFile

    //region generated code
    //@formatter:off
    @GeneratedCodeApiVersion(1)
    interface Builder: UnityPackageEntity, ModifiableWorkspaceEntity<UnityPackageEntity>, ObjBuilder<UnityPackageEntity> {
        override var descriptor: UnityPackage
        override var entitySource: EntitySource
        override var contentRootEntity: ContentRootEntity?
    }
    
    companion object: Type<UnityPackageEntity, Builder>() {
        operator fun invoke(descriptor: UnityPackage, entitySource: EntitySource, init: (Builder.() -> Unit)? = null): UnityPackageEntity {
            val builder = builder()
            builder.descriptor = descriptor
            builder.entitySource = entitySource
            init?.invoke(builder)
            return builder
        }
    }
    //@formatter:on
    //endregion

}
//region generated code
fun MutableEntityStorage.modifyEntity(entity: UnityPackageEntity, modification: UnityPackageEntity.Builder.() -> Unit) = modifyEntity(UnityPackageEntity.Builder::class.java, entity, modification)
var ContentRootEntity.Builder.unityPackageEntity: UnityPackageEntity?
    get() {
        return referrersx(UnityPackageEntity::contentRootEntity).singleOrNull()
    }
    set(value) {
        (this as ModifiableReferableWorkspaceEntity).linkExternalEntity(UnityPackageEntity::class, if (value is List<*>) value as List<WorkspaceEntity?> else listOf(value) as List<WorkspaceEntity?> )
    }

//endregion

@Suppress("unused")
private val ContentRootEntity.unityPackageEntity: UnityPackageEntity?
    get() = referrersx(UnityPackageEntity::contentRootEntity).singleOrNull()