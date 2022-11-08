@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.impl.toVirtualFile
import com.intellij.workspaceModel.storage.*
import com.intellij.workspaceModel.storage.EntityInformation
import com.intellij.workspaceModel.storage.EntitySource
import com.intellij.workspaceModel.storage.EntityStorage
import com.intellij.workspaceModel.storage.GeneratedCodeApiVersion
import com.intellij.workspaceModel.storage.GeneratedCodeImplVersion
import com.intellij.workspaceModel.storage.MutableEntityStorage
import com.intellij.workspaceModel.storage.WorkspaceEntity
import com.intellij.workspaceModel.storage.bridgeEntities.ContentRootEntity
import com.intellij.workspaceModel.storage.impl.ConnectionId
import com.intellij.workspaceModel.storage.impl.EntityLink
import com.intellij.workspaceModel.storage.impl.ModifiableWorkspaceEntityBase
import com.intellij.workspaceModel.storage.impl.UsedClassesCollector
import com.intellij.workspaceModel.storage.impl.WorkspaceEntityBase
import com.intellij.workspaceModel.storage.impl.WorkspaceEntityData
import com.intellij.workspaceModel.storage.impl.extractOneToOneChild
import com.intellij.workspaceModel.storage.impl.updateOneToOneChildOfParent
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityGitDetails
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource
import org.jetbrains.deft.ObjBuilder
import org.jetbrains.deft.Type
import org.jetbrains.deft.annotations.Child

@GeneratedCodeApiVersion(1)
@GeneratedCodeImplVersion(1)
open class UnityPackageEntityImpl(val dataSource: UnityPackageEntityData) : UnityPackageEntity, WorkspaceEntityBase() {

  companion object {
    internal val CONTENTROOTENTITY_CONNECTION_ID: ConnectionId = ConnectionId.create(UnityPackageEntity::class.java,
                                                                                     ContentRootEntity::class.java,
                                                                                     ConnectionId.ConnectionType.ONE_TO_ONE, true)

    val connections = listOf<ConnectionId>(
      CONTENTROOTENTITY_CONNECTION_ID,
    )

  }

  override val descriptor: UnityPackage
    get() = dataSource.descriptor

  override val contentRootEntity: ContentRootEntity?
    get() = snapshot.extractOneToOneChild(CONTENTROOTENTITY_CONNECTION_ID, this)

  override val entitySource: EntitySource
    get() = dataSource.entitySource

  override fun connectionIdList(): List<ConnectionId> {
    return connections
  }

  class Builder(result: UnityPackageEntityData?) : ModifiableWorkspaceEntityBase<UnityPackageEntity, UnityPackageEntityData>(
    result), UnityPackageEntity.Builder {
    constructor() : this(UnityPackageEntityData())

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
      // After adding entity data to the builder, we need to unbind it and move the control over entity data to builder
      // Builder may switch to snapshot at any moment and lock entity data to modification
      this.currentEntityData = null

      // Process linked entities that are connected without a builder
      processLinkedEntities(builder)
      checkInitialization() // TODO uncomment and check failed tests
    }

    fun checkInitialization() {
      val _diff = diff
      if (!getEntityData().isEntitySourceInitialized()) {
        error("Field WorkspaceEntity#entitySource should be initialized")
      }
      if (!getEntityData().isDescriptorInitialized()) {
        error("Field UnityPackageEntity#descriptor should be initialized")
      }
    }

    override fun connectionIdList(): List<ConnectionId> {
      return connections
    }

    // Relabeling code, move information from dataSource to this builder
    override fun relabel(dataSource: WorkspaceEntity, parents: Set<WorkspaceEntity>?) {
      dataSource as UnityPackageEntity
      if (this.entitySource != dataSource.entitySource) this.entitySource = dataSource.entitySource
      if (this.descriptor != dataSource.descriptor) this.descriptor = dataSource.descriptor
      if (parents != null) {
      }
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

    override var contentRootEntity: ContentRootEntity?
      get() {
        val _diff = diff
        return if (_diff != null) {
          _diff.extractOneToOneChild(CONTENTROOTENTITY_CONNECTION_ID, this) ?: this.entityLinks[EntityLink(true,
                                                                                                           CONTENTROOTENTITY_CONNECTION_ID)] as? ContentRootEntity
        }
        else {
          this.entityLinks[EntityLink(true, CONTENTROOTENTITY_CONNECTION_ID)] as? ContentRootEntity
        }
      }
      set(value) {
        checkModificationAllowed()
        val _diff = diff
        if (_diff != null && value is ModifiableWorkspaceEntityBase<*, *> && value.diff == null) {
          if (value is ModifiableWorkspaceEntityBase<*, *>) {
            value.entityLinks[EntityLink(false, CONTENTROOTENTITY_CONNECTION_ID)] = this
          }
          // else you're attaching a new entity to an existing entity that is not modifiable
          _diff.addEntity(value)
        }
        if (_diff != null && (value !is ModifiableWorkspaceEntityBase<*, *> || value.diff != null)) {
          _diff.updateOneToOneChildOfParent(CONTENTROOTENTITY_CONNECTION_ID, this, value)
        }
        else {
          if (value is ModifiableWorkspaceEntityBase<*, *>) {
            value.entityLinks[EntityLink(false, CONTENTROOTENTITY_CONNECTION_ID)] = this
          }
          // else you're attaching a new entity to an existing entity that is not modifiable

          this.entityLinks[EntityLink(true, CONTENTROOTENTITY_CONNECTION_ID)] = value
        }
        changedProperty.add("contentRootEntity")
      }

    override fun getEntityClass(): Class<UnityPackageEntity> = UnityPackageEntity::class.java
  }
}

class UnityPackageEntityData : WorkspaceEntityData<UnityPackageEntity>() {
  lateinit var descriptor: UnityPackage

  fun isDescriptorInitialized(): Boolean = ::descriptor.isInitialized

  override fun wrapAsModifiable(diff: MutableEntityStorage): WorkspaceEntity.Builder<UnityPackageEntity> {
    val modifiable = UnityPackageEntityImpl.Builder(null)
    modifiable.diff = diff
    modifiable.snapshot = diff
    modifiable.id = createEntityId()
    return modifiable
  }

  override fun createEntity(snapshot: EntityStorage): UnityPackageEntity {
    return getCached(snapshot) {
      val entity = UnityPackageEntityImpl(this)
      entity.snapshot = snapshot
      entity.id = createEntityId()
      entity
    }
  }

  override fun getEntityInterface(): Class<out WorkspaceEntity> {
    return UnityPackageEntity::class.java
  }

  override fun serialize(ser: EntityInformation.Serializer) {
  }

  override fun deserialize(de: EntityInformation.Deserializer) {
  }

  override fun createDetachedEntity(parents: List<WorkspaceEntity>): WorkspaceEntity {
    return UnityPackageEntity(descriptor, entitySource) {
    }
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

  override fun collectClassUsagesData(collector: UsedClassesCollector) {
    collector.add(UnityPackage::class.java)
    collector.add(UnityGitDetails::class.java)
    collector.add(UnityPackageSource::class.java)
    this.descriptor?.let { collector.addDataToInspect(it) }
    collector.sameForAllEntities = true
  }
}
