@file:JvmName("UnityPackageEntityModifications") @file:Suppress("UnstableApiUsage")

package com.jetbrains.rider.plugins.unity.workspace

import com.intellij.platform.workspace.storage.EntitySource
import com.intellij.platform.workspace.storage.EntityType
import com.intellij.platform.workspace.storage.GeneratedCodeApiVersion
import com.intellij.platform.workspace.storage.MutableEntityStorage
import com.intellij.platform.workspace.storage.WorkspaceEntityBuilder
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage

@GeneratedCodeApiVersion(3)
interface UnityPackageEntityBuilder : WorkspaceEntityBuilder<UnityPackageEntity> {
    override var entitySource: EntitySource
    var descriptor: UnityPackage
}

internal object UnityPackageEntityType : EntityType<UnityPackageEntity, UnityPackageEntityBuilder>() {
    override val entityClass: Class<UnityPackageEntity> get() = UnityPackageEntity::class.java
    operator fun invoke(
        descriptor: UnityPackage,
        entitySource: EntitySource,
        init: (UnityPackageEntityBuilder.() -> Unit)? = null,
    ): UnityPackageEntityBuilder {
        val builder = builder()
        builder.descriptor = descriptor
        builder.entitySource = entitySource
        init?.invoke(builder)
        return builder
    }
}

fun MutableEntityStorage.modifyUnityPackageEntity(
    entity: UnityPackageEntity,
    modification: UnityPackageEntityBuilder.() -> Unit,
): UnityPackageEntity = modifyEntity(UnityPackageEntityBuilder::class.java, entity, modification)

@JvmOverloads
@JvmName("createUnityPackageEntity")
fun UnityPackageEntity(
    descriptor: UnityPackage,
    entitySource: EntitySource,
    init: (UnityPackageEntityBuilder.() -> Unit)? = null,
): UnityPackageEntityBuilder = UnityPackageEntityType(descriptor, entitySource, init)
