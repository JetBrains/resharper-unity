@file:OptIn(EntityStorageInstrumentationApi::class)

package com.jetbrains.rider.plugins.unity.workspace.impl

import com.intellij.platform.workspace.storage.ConnectionId
import com.intellij.platform.workspace.storage.EntitySource
import com.intellij.platform.workspace.storage.GeneratedCodeApiVersion
import com.intellij.platform.workspace.storage.GeneratedCodeImplVersion
import com.intellij.platform.workspace.storage.WorkspaceEntity
import com.intellij.platform.workspace.storage.WorkspaceEntityBuilder
import com.intellij.platform.workspace.storage.WorkspaceEntityInternalApi
import com.intellij.platform.workspace.storage.impl.ModifiableWorkspaceEntityBase
import com.intellij.platform.workspace.storage.impl.WorkspaceEntityBase
import com.intellij.platform.workspace.storage.impl.WorkspaceEntityData
import com.intellij.platform.workspace.storage.instrumentation.EntityStorageInstrumentationApi
import com.intellij.platform.workspace.storage.metadata.model.EntityMetadata
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity
import com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntityBuilder

@GeneratedCodeApiVersion(3)
@GeneratedCodeImplVersion(7)
@OptIn(WorkspaceEntityInternalApi::class)
internal class UnityPackageEntityImpl(private val dataSource: UnityPackageEntityData) : UnityPackageEntity,
    WorkspaceEntityBase(dataSource) {

    override val descriptor: UnityPackage
        get() {
            readField("descriptor")
            return dataSource.descriptor
        }
    override val entitySource: EntitySource
        get() {
            readField("entitySource")
            return dataSource.entitySource
        }

    override fun connectionIdList(): List<ConnectionId> {
        return emptyList()
    }

    internal class Builder(result: UnityPackageEntityData?) :
        ModifiableWorkspaceEntityBase<UnityPackageEntity, UnityPackageEntityData>(result), UnityPackageEntityBuilder {
        internal constructor() : this(UnityPackageEntityData())

        override fun checkInitialization() {
            val _diff = diff
            if (!getEntityData().isEntitySourceInitialized()) {
                error("Field WorkspaceEntity#entitySource should be initialized")
            }
            if (!getEntityData().isDescriptorInitialized()) {
                error("Field UnityPackageEntity#descriptor should be initialized")
            }
        }

        override fun connectionIdList(): List<ConnectionId> {
            return emptyList()
        }

        // Relabeling code, move information from dataSource to this builder
        override fun relabel(dataSource: WorkspaceEntity, parents: Set<WorkspaceEntity>?) {
            dataSource as UnityPackageEntity
            if (this.entitySource != dataSource.entitySource) this.entitySource = dataSource.entitySource
            if (this.descriptor != dataSource.descriptor) this.descriptor = dataSource.descriptor
            updateChildToParentReferences(parents)
        }

        override var entitySource: EntitySource
            get() = getEntityData().entitySource
            set(value) {
                checkModificationAllowed()
                getEntityData(true).entitySource = value
                changedProperty.add("entitySource")
            }
        override var descriptor: UnityPackage
            get() = getEntityData().descriptor
            set(value) {
                checkModificationAllowed()
                getEntityData(true).descriptor = value
                changedProperty.add("descriptor")
            }

        override fun getEntityClass(): Class<UnityPackageEntity> = UnityPackageEntity::class.java
    }
}

@OptIn(WorkspaceEntityInternalApi::class)
internal class UnityPackageEntityData : WorkspaceEntityData<UnityPackageEntity>() {
    lateinit var descriptor: UnityPackage
    internal fun isDescriptorInitialized(): Boolean = ::descriptor.isInitialized
    override fun newInstance(): UnityPackageEntity = UnityPackageEntityImpl(this)
    override fun newBuilderInstance(): ModifiableWorkspaceEntityBase<UnityPackageEntity, *> = UnityPackageEntityImpl.Builder(null)
    override fun getMetadata(): EntityMetadata {
        return MetadataStorageImpl.getMetadataByTypeFqn("com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity") as EntityMetadata
    }

    override fun getEntityInterface(): Class<out WorkspaceEntity> {
        return UnityPackageEntity::class.java
    }

    override fun createDetachedEntity(parents: List<WorkspaceEntityBuilder<*>>): WorkspaceEntityBuilder<*> {
        return UnityPackageEntity(descriptor, entitySource)
    }

    override fun getRequiredParents(): List<Class<out WorkspaceEntity>> {
        val res = mutableListOf<Class<out WorkspaceEntity>>()
        return res
    }

    override fun equals(other: Any?): Boolean {
        if (other == null) return false
        if (this.javaClass != other.javaClass) return false
        other as UnityPackageEntityData
        if (this.entitySource != other.entitySource) return false
        if (this.descriptor != other.descriptor) return false
        return true
    }

    override fun equalsIgnoringEntitySource(other: Any?): Boolean {
        if (other == null) return false
        if (this.javaClass != other.javaClass) return false
        other as UnityPackageEntityData
        if (this.descriptor != other.descriptor) return false
        return true
    }

    override fun hashCode(): Int {
        var result = entitySource.hashCode()
        result = 31 * result + descriptor.hashCode()
        return result
    }

    override fun hashCodeIgnoringEntitySource(): Int {
        var result = javaClass.hashCode()
        result = 31 * result + descriptor.hashCode()
        return result
    }
}
