package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.deft.api.annotations.Ignore
import com.intellij.workspaceModel.ide.impl.virtualFile
import com.intellij.workspaceModel.storage.*
import com.intellij.workspaceModel.storage.EntityInformation
import com.intellij.workspaceModel.storage.EntitySource
import com.intellij.workspaceModel.storage.EntityStorage
import com.intellij.workspaceModel.storage.GeneratedCodeApiVersion
import com.intellij.workspaceModel.storage.GeneratedCodeImplVersion
import com.intellij.workspaceModel.storage.ModifiableWorkspaceEntity
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.WorkspaceEntity
import com.intellij.workspaceModel.storage.bridgeEntities.api.ContentRootEntity
import com.intellij.workspaceModel.storage.impl.ConnectionId
import com.intellij.workspaceModel.storage.impl.ExtRefKey
import com.intellij.workspaceModel.storage.impl.ModifiableWorkspaceEntityBase
import com.intellij.workspaceModel.storage.impl.WorkspaceEntityBase
import com.intellij.workspaceModel.storage.impl.WorkspaceEntityData
import com.intellij.workspaceModel.storage.impl.extractOneToOneParent
import com.intellij.workspaceModel.storage.impl.updateOneToOneParentOfChild
import com.intellij.workspaceModel.storage.referrersx
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource
import org.jetbrains.deft.ObjBuilder
import org.jetbrains.deft.Type

@GeneratedCodeApiVersion(0)
@GeneratedCodeImplVersion(0)
open class UnityPackageEntityImpl: UnityPackageEntity, WorkspaceEntityBase() {
    
    companion object {
        internal val CONTENTROOTENTITY_CONNECTION_ID: ConnectionId = ConnectionId.create(ContentRootEntity::class.java, UnityPackageEntity::class.java, ConnectionId.ConnectionType.ONE_TO_ONE, true)
    }
        
    @JvmField var _descriptor: UnityPackage? = null
    override val descriptor: UnityPackage
        get() = _descriptor!!
                        
    override val contentRootEntity: ContentRootEntity?
        get() = snapshot.extractOneToOneParent(CONTENTROOTENTITY_CONNECTION_ID, this)

    class Builder(val result: UnityPackageEntityData?): ModifiableWorkspaceEntityBase<UnityPackageEntity>(), UnityPackageEntity.Builder {
        constructor(): this(UnityPackageEntityData())
        
        override fun applyToBuilder(builder: MutableEntityStorage) {
            if (this.diff != null) {
                if (existsInBuilder(builder)) {
                    this.diff = builder
                    return
                }
                else {
                    error("Entity UnityPackageEntity is already created in a different builder")
                }
            }
            
            this.diff = builder
            this.snapshot = builder
            addToBuilder()
            this.id = getEntityData().createEntityId()
            
            // Process entities from extension fields
            val keysToRemove = ArrayList<ExtRefKey>()
            for ((key, entity) in extReferences) {
                if (!key.isChild()) {
                    continue
                }
                if (entity is List<*>) {
                    for (item in entity) {
                        if (item is ModifiableWorkspaceEntityBase<*>) {
                            builder.addEntity(item)
                        }
                    }
                    entity as List<WorkspaceEntity>
                    val (withBuilder_entity, woBuilder_entity) = entity.partition { it is ModifiableWorkspaceEntityBase<*> && it.diff != null }
                    applyRef(key.getConnectionId(), withBuilder_entity)
                    keysToRemove.add(key)
                }
                else {
                    entity as WorkspaceEntity
                    builder.addEntity(entity)
                    applyRef(key.getConnectionId(), entity)
                    keysToRemove.add(key)
                }
            }
            for (key in keysToRemove) {
                extReferences.remove(key)
            }
            
            // Adding parents and references to the parent
            val __contentRootEntity = _contentRootEntity
            if (__contentRootEntity != null && (__contentRootEntity is ModifiableWorkspaceEntityBase<*>) && __contentRootEntity.diff == null) {
                builder.addEntity(__contentRootEntity)
            }
            if (__contentRootEntity != null && (__contentRootEntity is ModifiableWorkspaceEntityBase<*>) && __contentRootEntity.diff != null) {
                // Set field to null (in referenced entity)
                __contentRootEntity.extReferences.remove(ExtRefKey("UnityPackageEntity", "contentRootEntity", true, CONTENTROOTENTITY_CONNECTION_ID))
            }
            if (__contentRootEntity != null) {
                applyParentRef(CONTENTROOTENTITY_CONNECTION_ID, __contentRootEntity)
                this._contentRootEntity = null
            }
            val parentKeysToRemove = ArrayList<ExtRefKey>()
            for ((key, entity) in extReferences) {
                if (key.isChild()) {
                    continue
                }
                if (entity is List<*>) {
                    error("Cannot have parent lists")
                }
                else {
                    entity as WorkspaceEntity
                    builder.addEntity(entity)
                    applyParentRef(key.getConnectionId(), entity)
                    parentKeysToRemove.add(key)
                }
            }
            for (key in parentKeysToRemove) {
                extReferences.remove(key)
            }
            checkInitialization() // TODO uncomment and check failed tests
        }
    
        fun checkInitialization() {
            val _diff = diff
            if (!getEntityData().isDescriptorInitialized()) {
                error("Field UnityPackageEntity#descriptor should be initialized")
            }
            if (!getEntityData().isEntitySourceInitialized()) {
                error("Field UnityPackageEntity#entitySource should be initialized")
            }
        }
    
        
        override var descriptor: UnityPackage
            get() = getEntityData().descriptor
            set(value) {
                checkModificationAllowed()
                getEntityData().descriptor = value
                changedProperty.add("descriptor")
                
            }
            
        override var entitySource: EntitySource
            get() = getEntityData().entitySource
            set(value) {
                checkModificationAllowed()
                getEntityData().entitySource = value
                changedProperty.add("entitySource")
                
            }
            
        var _contentRootEntity: ContentRootEntity? = null
        override var contentRootEntity: ContentRootEntity?
            get() {
                val _diff = diff
                return if (_diff != null) {
                    _diff.extractOneToOneParent(CONTENTROOTENTITY_CONNECTION_ID, this) ?: _contentRootEntity
                } else {
                    _contentRootEntity
                }
            }
            set(value) {
                checkModificationAllowed()
                val _diff = diff
                if (_diff != null && value is ModifiableWorkspaceEntityBase<*> && value.diff == null) {
                    // Back reference for an optional of ext field
                    if (value is ModifiableWorkspaceEntityBase<*>) {
                        value.extReferences[ExtRefKey("UnityPackageEntity", "contentRootEntity", false, CONTENTROOTENTITY_CONNECTION_ID)] = this
                    }
                    // else you're attaching a new entity to an existing entity that is not modifiable
                    _diff.addEntity(value)
                }
                if (_diff != null && (value !is ModifiableWorkspaceEntityBase<*> || value.diff != null)) {
                    _diff.updateOneToOneParentOfChild(CONTENTROOTENTITY_CONNECTION_ID, this, value)
                }
                else {
                    // Back reference for an optional of ext field
                    if (value is ModifiableWorkspaceEntityBase<*>) {
                        value.extReferences[ExtRefKey("UnityPackageEntity", "contentRootEntity", false, CONTENTROOTENTITY_CONNECTION_ID)] = this
                    }
                    // else you're attaching a new entity to an existing entity that is not modifiable
                    
                    this._contentRootEntity = value
                }
                changedProperty.add("contentRootEntity")
            }
        
        override fun getEntityData(): UnityPackageEntityData = result ?: super.getEntityData() as UnityPackageEntityData
        override fun getEntityClass(): Class<UnityPackageEntity> = UnityPackageEntity::class.java
    }
}
    
class UnityPackageEntityData : WorkspaceEntityData<UnityPackageEntity>() {
    lateinit var descriptor: UnityPackage

    fun isDescriptorInitialized(): Boolean = ::descriptor.isInitialized

    override fun wrapAsModifiable(diff: MutableEntityStorage): ModifiableWorkspaceEntity<UnityPackageEntity> {
        val modifiable = UnityPackageEntityImpl.Builder(null)
        modifiable.allowModifications {
          modifiable.diff = diff
          modifiable.snapshot = diff
          modifiable.id = createEntityId()
          modifiable.entitySource = this.entitySource
        }
        return modifiable
    }

    override fun createEntity(snapshot: EntityStorage): UnityPackageEntity {
        val entity = UnityPackageEntityImpl()
        entity._descriptor = descriptor
        entity.entitySource = entitySource
        entity.snapshot = snapshot
        entity.id = createEntityId()
        return entity
    }

    override fun getEntityInterface(): Class<out WorkspaceEntity> {
        return UnityPackageEntity::class.java
    }

    override fun serialize(ser: EntityInformation.Serializer) {
    }

    override fun deserialize(de: EntityInformation.Deserializer) {
    }

    override fun equals(other: Any?): Boolean {
        if (other == null) return false
        if (this::class != other::class) return false
        
        other as UnityPackageEntityData
        
        if (this.descriptor != other.descriptor) return false
        if (this.entitySource != other.entitySource) return false
        return true
    }

    override fun equalsIgnoringEntitySource(other: Any?): Boolean {
        if (other == null) return false
        if (this::class != other::class) return false
        
        other as UnityPackageEntityData
        
        if (this.descriptor != other.descriptor) return false
        return true
    }

    override fun hashCode(): Int {
        var result = entitySource.hashCode()
        result = 31 * result + descriptor.hashCode()
        return result
    }
}