@file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.openapi.vfs.VirtualFile
import com.intellij.platform.backend.workspace.virtualFile
import com.intellij.platform.workspace.jps.entities.ContentRootEntity
import com.intellij.platform.workspace.storage.*
import com.intellij.platform.workspace.storage.EntitySource
import com.intellij.platform.workspace.storage.EntityType
import com.intellij.platform.workspace.storage.GeneratedCodeApiVersion
import com.intellij.platform.workspace.storage.MutableEntityStorage
import com.intellij.platform.workspace.storage.WorkspaceEntity
import com.intellij.platform.workspace.storage.annotations.Child
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

  @Child
  val contentRootEntity: ContentRootEntity?

  val packageFolder: VirtualFile? get() = contentRootEntity?.url?.virtualFile

  //region generated code
  @GeneratedCodeApiVersion(2)
  interface Builder : UnityPackageEntity, WorkspaceEntity.Builder<UnityPackageEntity> {
    override var entitySource: EntitySource
    override var descriptor: UnityPackage
    override var contentRootEntity: ContentRootEntity?
  }

  companion object : EntityType<UnityPackageEntity, Builder>() {
    @JvmOverloads
    @JvmStatic
    @JvmName("create")
    operator fun invoke(descriptor: UnityPackage, entitySource: EntitySource, init: (Builder.() -> Unit)? = null): UnityPackageEntity {
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
fun MutableEntityStorage.modifyEntity(entity: UnityPackageEntity,
                                      modification: UnityPackageEntity.Builder.() -> Unit): UnityPackageEntity = modifyEntity(
  UnityPackageEntity.Builder::class.java, entity, modification)

var ContentRootEntity.Builder.unityPackageEntity: UnityPackageEntity?
  by WorkspaceEntity.extension()
//endregion

@Suppress("unused")
private val ContentRootEntity.unityPackageEntity: UnityPackageEntity? by WorkspaceEntity.extension()