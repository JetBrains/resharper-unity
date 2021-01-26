package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uxml.codeInsight.schema

import com.intellij.openapi.module.Module
import com.intellij.openapi.project.DumbAware
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
import com.jetbrains.rd.platform.util.idea.getOrCreateUserData
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.projectDir
import java.nio.file.Paths

class UxmlSchemaProvider: XmlSchemaProvider(), DumbAware {
    private val SCHEMAS_FILE_MAP_KEY: Key<MutableMap<String, CachedValue<XmlFile>>> = Key.create("UXML_SCHEMAS_FILE_MAP_KEY")

    override fun isAvailable(file: XmlFile): Boolean {
        // Add schemas for any XML file type in a Unity project. This means schemas will resolve inside the .xsd files too
        return file.project.isUnityProject()
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

    override fun getAvailableNamespaces(file: XmlFile, tagName: String?): Set<String> {
        // For the given XmlFile, what namespaces do we know about?
        // The schemas are named after the namespace
        return getSchemas(file.project).keys
    }

    override fun getLocations(namespace: String, context: XmlFile): Set<String>? {
        val schemas = getSchemas(context.project)
        if (schemas.containsKey(namespace)) {
            return mutableSetOf(Paths.get(context.project.projectDir.canonicalPath, "UIElementsSchema", "$namespace.xsd").toUri().toString())
        }

        return null
    }

    private fun getSchemas(project: Project): MutableMap<String, CachedValue<XmlFile>> {
        return project.getOrCreateUserData(SCHEMAS_FILE_MAP_KEY) { mutableMapOf() }
    }
}