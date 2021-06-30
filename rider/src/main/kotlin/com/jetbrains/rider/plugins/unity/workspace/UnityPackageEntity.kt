@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.impl.virtualFile
import com.intellij.workspaceModel.storage.WorkspaceEntityStorage
import com.intellij.workspaceModel.storage.bridgeEntities.ContentRootEntity
import com.intellij.workspaceModel.storage.impl.EntityDataDelegation
import com.intellij.workspaceModel.storage.impl.ModifiableWorkspaceEntityBase
import com.intellij.workspaceModel.storage.impl.WorkspaceEntityBase
import com.intellij.workspaceModel.storage.impl.WorkspaceEntityData
import com.intellij.workspaceModel.storage.impl.references.*
import com.intellij.workspaceModel.storage.url.VirtualFileUrl
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource

class UnityPackageEntity(private val descriptor: UnityPackage) : WorkspaceEntityBase() {

    companion object {
        private val contentRootDelegate = OneToOneParent.Nullable<UnityPackageEntity, ContentRootEntity>(
            ContentRootEntity::class.java,
            isParentInChildNullable = true)
    }

    val id: String get() = descriptor.id
    val version: String get() = descriptor.version
    val source: UnityPackageSource get() = descriptor.source
    val displayName: String get() = descriptor.displayName
    val description: String? get() = descriptor.description
    val dependencies: Map<String, String> get() = descriptor.dependencies.associate { it.id to it.version }
    val tarballLocation: String? get() = descriptor.tarballLocation
    val gitUrl: String? get() = descriptor.gitDetails?.url
    val gitHash: String? get() = descriptor.gitDetails?.hash
    val gitRevision: String?  get() = descriptor.gitDetails?.revision

    fun isEditable(): Boolean {
        return descriptor.source in arrayOf(UnityPackageSource.Embedded, UnityPackageSource.Local)
    }

    fun isReadOnly(): Boolean {
        return !isEditable() && descriptor.source != UnityPackageSource.Unknown
    }

    val contentRootEntity: ContentRootEntity? by contentRootDelegate
    val packageFolder: VirtualFile? get() = contentRootEntity?.url?.virtualFile

    override fun toString(): String {
        return "${descriptor.id} ${descriptor.javaClass.simpleName} InternalId=${super.toString()}"
    }
}

class ModifiableUnityPackageEntity : ModifiableWorkspaceEntityBase<UnityPackageEntity>() {
    var descriptor: UnityPackage by EntityDataDelegation()

    var contentRootEntity: ContentRootEntity? by MutableOneToOneParent.Nullable(
        UnityPackageEntity::class.java,
        ContentRootEntity::class.java,
        isParentInChildNullable = true
    )
}

@Suppress("unused")
class UnityPackageEntityData : WorkspaceEntityData<UnityPackageEntity>() {

    lateinit var descriptor: UnityPackage

    var url: VirtualFileUrl? = null

    override fun createEntity(snapshot: WorkspaceEntityStorage): UnityPackageEntity {
        return UnityPackageEntity(descriptor).also {
            addMetaData(it, snapshot)
        }
    }

    override fun equals(other: Any?): Boolean {
        return equalsIgnoringEntitySource(other)
            && entitySource == (other as UnityPackageEntityData).entitySource
    }

    override fun equalsIgnoringEntitySource(other: Any?): Boolean {
        if (other == null) return false
        if (this === other) return true
        if (this::class != other::class) return false

        val projectModelEntityData = other as UnityPackageEntityData
        return descriptor == projectModelEntityData.descriptor
            && url == projectModelEntityData.url
    }

    override fun hashCode(): Int {
        var hash = 7
        hash = 31 * hash + entitySource.hashCode()
        hash = 31 * hash + descriptor.hashCode()
        hash = 31 * hash + (url?.hashCode() ?: 0)
        return hash
    }
}