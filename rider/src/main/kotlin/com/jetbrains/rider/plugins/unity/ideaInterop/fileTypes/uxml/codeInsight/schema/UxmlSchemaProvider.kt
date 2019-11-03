package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml.codeInsight.schema

import com.intellij.openapi.module.Module
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.util.ModificationTracker
import com.intellij.psi.PsiFile
import com.intellij.psi.PsiManager
import com.intellij.psi.util.CachedValue
import com.intellij.psi.util.CachedValueProvider
import com.intellij.psi.util.CachedValuesManager
import com.intellij.psi.xml.XmlFile
import com.intellij.xml.XmlSchemaProvider
import com.jetbrains.rdclient.util.idea.getOrCreateUserData
import com.jetbrains.rider.projectDir

class UxmlSchemaProvider: XmlSchemaProvider() {
    private val SCHEMAS_FILE_MAP_KEY: Key<MutableMap<String, CachedValue<XmlFile>>> = Key.create("UXML_SCHEMAS_FILE_MAP_KEY")

    override fun isAvailable(file: XmlFile): Boolean {
        return file.name.endsWith(".uxml", true)
    }

    override fun getSchema(url: String, module: Module?, baseFile: PsiFile): XmlFile? {
        // Load the schema for the given url. Basically, this will be /UIElementSchema/{url}.xsd
        if (url.isEmpty()) return null

        val project = baseFile.project
        val schemas = getSchemas(project)

        val cachedValue = schemas[url]
        if (cachedValue != null) return cachedValue.value

        val schema = CachedValuesManager.getManager(project).createCachedValue(object : CachedValueProvider<XmlFile> {
            override fun compute(): CachedValueProvider.Result<XmlFile>? {
                val file = project.projectDir.findFileByRelativePath("/UIElementsSchema/$url.xsd")
                if (file == null || !file.exists()) {
                    return CachedValueProvider.Result(null, ModificationTracker.EVER_CHANGED)
                }

                val psiFile = PsiManager.getInstance(project).findFile(file)

                return CachedValueProvider.Result.create(psiFile as XmlFile, psiFile, file)
            }
        }, true)

        schemas[url] = schema
        return schema.value
    }

    override fun getAvailableNamespaces(file: XmlFile, tagName: String?): MutableSet<String> {
        // For the given XmlFile, what namespaces do we know about?
        if (!file.name.endsWith(".uxml", true)) {
            return mutableSetOf()
        }

        // The schemas are named after the namespace
        val project = file.project
        return getSchemas(project).keys
    }

    private fun getSchemas(project: Project): MutableMap<String, CachedValue<XmlFile>> {
        return project.getOrCreateUserData(SCHEMAS_FILE_MAP_KEY) { mutableMapOf() }
    }
}