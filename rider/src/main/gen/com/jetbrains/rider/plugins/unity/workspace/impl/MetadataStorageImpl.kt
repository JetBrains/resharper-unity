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
        val primitiveTypeListNotNullable = ValueTypeMetadata.SimpleType.PrimitiveType(isNullable = false, type = "List")
        val primitiveTypeStringNullable = ValueTypeMetadata.SimpleType.PrimitiveType(isNullable = true, type = "String")

        var typeMetadata: StorageTypeMetadata

        typeMetadata = EntityMetadata(fqName = "com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity", entityDataFqName = "com.jetbrains.rider.plugins.unity.workspace.impl.UnityPackageEntityData", supertypes = listOf("com.intellij.platform.workspace.storage.WorkspaceEntity"), properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "entitySource", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.KnownClass(fqName = "com.intellij.platform.workspace.storage.EntitySource")), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "descriptor", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.ClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage", properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "dependencies", valueType = ValueTypeMetadata.ParameterizedType(generics = listOf(ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.ClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageDependency", properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "id", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "version", valueType = primitiveTypeStringNotNullable, withDefault = false)), supertypes = listOf("com.jetbrains.rd.util.string.IPrintable")))), primitive = primitiveTypeListNotNullable), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "description", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "displayName", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "gitDetails", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = true, typeMetadata = FinalClassMetadata.ClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityGitDetails", properties = listOf(OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "hash", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "revision", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "url", valueType = primitiveTypeStringNotNullable, withDefault = false)), supertypes = listOf("com.jetbrains.rd.util.string.IPrintable"))), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "id", valueType = primitiveTypeStringNotNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "packageFolderPath", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "source", valueType = ValueTypeMetadata.SimpleType.CustomType(isNullable = false, typeMetadata = FinalClassMetadata.EnumClassMetadata(fqName = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource", properties = listOf(), supertypes = listOf("java.io.Serializable",
"kotlin.Comparable",
"kotlin.Enum"), values = listOf("BuiltIn",
"Embedded",
"Git",
"Local",
"LocalTarball",
"Registry",
"Unknown"))), withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "tarballLocation", valueType = primitiveTypeStringNullable, withDefault = false),
OwnPropertyMetadata(isComputable = false, isKey = false, isOpen = false, name = "version", valueType = primitiveTypeStringNotNullable, withDefault = false)), supertypes = listOf("com.jetbrains.rd.util.string.IPrintable"))), withDefault = false)), extProperties = listOf(), isAbstract = false)

        addMetadata(typeMetadata)
    }

    override fun initializeMetadataHash() {
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.workspace.UnityPackageEntity", metadataHash = 277233226)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackage", metadataHash = 363072722)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageDependency", metadataHash = 333232422)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityGitDetails", metadataHash = -1563565493)
        addMetadataHash(typeFqn = "com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityPackageSource", metadataHash = 429756747)
    }

}
