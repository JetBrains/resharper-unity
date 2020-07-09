package com.jetbrains.rider.plugins.unity.asmdef

import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.jsonSchema.extension.JsonSchemaFileProvider
import com.jetbrains.jsonSchema.extension.JsonSchemaProviderFactory
import com.jetbrains.jsonSchema.extension.SchemaType
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef.AsmDefFileType

class AsmDefJsonSchemeProviderFactory : JsonSchemaProviderFactory {
    override fun getProviders(p0: Project): MutableList<JsonSchemaFileProvider> {
        return mutableListOf(
            object : JsonSchemaFileProvider {
                private val schemaFile = JsonSchemaProviderFactory.getResourceFile(this::class.java, "/schemas/unity/asmdef.json")
                override fun isAvailable(file: VirtualFile) = file.fileType == AsmDefFileType
                override fun getName() = "Unity Assembly Definition"
                override fun getSchemaFile() = schemaFile
                override fun getSchemaType() = SchemaType.embeddedSchema
                override fun getRemoteSource() = "https://json.schemastore.org/asmdef.json"
            })
    }
}