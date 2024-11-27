package com.jetbrains.rider.plugins.unity.workspace.impl

import com.intellij.platform.workspace.storage.WorkspaceEntityInternalApi
import com.intellij.platform.workspace.storage.metadata.impl.MetadataStorageBase
import com.intellij.platform.workspace.storage.metadata.model.EntityMetadata
import com.intellij.platform.workspace.storage.metadata.model.FinalClassMetadata
import com.intellij.platform.workspace.storage.metadata.model.OwnPropertyMetadata
import com.intellij.platform.workspace.storage.metadata.model.StorageTypeMetadata
import com.intellij.platform.workspace.storage.metadata.model.ValueTypeMetadata

@OptIn(WorkspaceEntityInternalApi::class)
internal object MetadataStorageImpl: MetadataStorageBase() {
    override fun initializeMetadata() {
        val primitiveTypeStringNotNullable = ValueTypeMetadata.SimpleType.PrimitiveType(isNullable = false, type = "String")
        val primitiveTypeStringNullable = ValueTypeMetadata.SimpleType.PrimitiveType(isNullable = true, type = "String")
        
        var typeMetadata: StorageTypeMetadata
        
        typeMetadata = FinalClassMetadata.ObjectMetadata(fqName = "com.jetbrains.rider.plugins.unity.workspace.UnityWorkspacePackageUpdater\$RiderUnityPackageEntitySource", properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "virtualFileUrl", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = true, typeMetadata = FinalClassMetadata.KnownClass(fqName = "com.intellij.platform.workspace.storage.url.VirtualFileUrl")), withDefault = false)), supertypes = listOf("com.jetbrains.rider.projectView.workspace.RiderEntitySource",
"com.intellij.platform.workspace.storage.EntitySource"))
        
        addMetadata(typeMetadata)
        
        typeMetadata = EntityMetadata(fqName = "com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity", entityDataFqName = "com.jetbrains.rider.plugins.unity.workspace.impl.UnityPackageEntityData", supertypes = listOf("com.intellij.platform.workspace.storage.WorkspaceEntity"), properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "entitySource", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.KnownClass(fqName = "com.intellij.platform.workspace.storage.EntitySource")), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "descriptor", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.ClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage", properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "id", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "version", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "packageFolderPath", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "source", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.EnumClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource", properties = listOf(), supertypes = listOf("kotlin.Enum",
"kotlin.Comparable",
"java.io.Serializable"), values = listOf("Unknown",
"BuiltIn",
"Registry",
"Embedded",
"Local",
"LocalTarball",
"Git"))), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "displayName", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "description", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "dependencies", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.KnownClass(fqName = "kotlin.Array")), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "tarballLocation", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "gitDetails", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = true, typeMetadata = FinalClassMetadata.ClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityGitDetails", properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "url", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "hash", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "revision", valueType = primitiveTypeStringNullable, withDefault = false)), supertypes = listOf("com.jetbrains.rd.util.string.IPrintable"))), withDefault = false)), supertypes = listOf("com.jetbrains.rd.util.string.IPrintable"))), withDefault = false)), extProperties = listOf(), isAbstract = false)
        
        addMetadata(typeMetadata)
    }

    override fun initializeMetadataHash() {
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity", metadataHash = 193016699)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage", metadataHash = -572504383)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource", metadataHash = -217023979)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityGitDetails", metadataHash = -55238709)
        addMetadataHash(typeFqn = "com.intellij.platform.workspace.storage.EntitySource", metadataHash = -1030321237)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.workspace.UnityWorkspacePackageUpdater\$RiderUnityPackageEntitySource", metadataHash = -1748851553)
    }

}
