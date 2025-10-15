package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.workspace.storage.*
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource

@GeneratedCodeApiVersion(3)
interface ModifiableUnityPackageEntity : ModifiableWorkspaceEntity<UnityPackageEntity> {
  override var entitySource: EntitySource
  var descriptor: UnityPackage
}

internal object UnityPackageEntityType : EntityType<UnityPackageEntity, ModifiableUnityPackageEntity>() {
  override val entityClass: Class<UnityPackageEntity> get() = UnityPackageEntity::class.java
  operator fun invoke(
    descriptor: UnityPackage,
    entitySource: EntitySource,
    init: (ModifiableUnityPackageEntity.() -> Unit)? = null,
  ): ModifiableUnityPackageEntity {
    val builder = builder()
    builder.descriptor = descriptor
    builder.entitySource = entitySource
    init?.invoke(builder)
    return builder
  }
}

fun MutableEntityStorage.modifyUnityPackageEntity(
  entity: UnityPackageEntity,
  modification: ModifiableUnityPackageEntity.() -> Unit,
): UnityPackageEntity = modifyEntity(ModifiableUnityPackageEntity::class.java, entity, modification)

@JvmOverloads
@JvmName("createUnityPackageEntity")
fun UnityPackageEntity(
  descriptor: UnityPackage,
  entitySource: EntitySource,
  init: (ModifiableUnityPackageEntity.() -> Unit)? = null,
): ModifiableUnityPackageEntity = UnityPackageEntityType(descriptor, entitySource, init)
