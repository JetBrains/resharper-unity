package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.openapi.util.JDOMUtil
import com.intellij.openapi.util.Version
import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.css.impl.descriptor.value.CssGroupValue
import com.intellij.psi.css.impl.descriptor.value.CssNameValue
import com.intellij.psi.css.impl.descriptor.value.CssTextValue
import com.intellij.psi.css.impl.descriptor.value.CssValueDescriptorVisitorImpl
import com.intellij.psi.css.impl.util.scheme.CssDescriptorsHolder
import com.intellij.psi.css.impl.util.scheme.CssDescriptorsLoader
import com.intellij.psi.css.impl.util.scheme.CssElementDescriptorFactory2
import com.intellij.reference.SoftReference.dereference
import java.lang.ref.Reference
import java.lang.ref.SoftReference
import java.net.URL
import java.util.Properties

@Service(Service.Level.APP)
internal class UssCssElementDescriptorFactory {
    companion object {
        private const val USS_SCHEMA = "/uss/element-descriptors.xml"
        private const val UNITY_SINCE_VERSIONS = "/uss/unity-since.properties"

        fun getInstance(): UssCssElementDescriptorFactory = service()
    }

    private var cssDescriptorsHolderRef: Reference<CssDescriptorsHolder>? = null
    private var valueIdentifiersRef: Reference<Set<String>>? = null
    private var unitySinceVersionsRef: Reference<Map<String, Version>>? = null

    fun getDescriptors(): CssDescriptorsHolder {
        var descriptors = dereference(cssDescriptorsHolderRef)
        if (descriptors == null) {
            val loader = CssDescriptorsLoader()
            val schemesToLoad = listOf("webref/css-cascade.xml", "webref/css-transitions.xml", "webref/css-transforms.xml", "webref/css-transforms-2.xml", "webref/css-color.xml")
            schemesToLoad.forEach {
                CssElementDescriptorFactory2.getInstance().javaClass.getResource("syntax-data/$it")!!.let { url -> loader.loadDescriptors(url) }
            }
            val ussSchema = this::class.java.getResource(USS_SCHEMA)!!
            // The USS definition of an id replaces the webref definition, so that only the Unity syntax applies
            removeRedeclaredIds(loader.descriptors, ussSchema)
            loader.loadDescriptors(ussSchema)
            descriptors = loader.descriptors
            cssDescriptorsHolderRef = SoftReference(descriptors)
        }
        return descriptors
    }

    /**
     * Maps a lower-case property id to the Unity version that introduced the property.
     * The data comes from `unity-since.properties`, because css.xsd does not allow a custom attribute in the USS schema.
     */
    fun getUnitySinceVersions(): Map<String, Version> {
        var versions = dereference(unitySinceVersionsRef)
        if (versions == null) {
            val properties = Properties()
            this::class.java.getResourceAsStream(UNITY_SINCE_VERSIONS)!!.use { properties.load(it) }
            versions = properties.stringPropertyNames().associate { id ->
                val since = properties.getProperty(id).trim()
                StringUtil.toLowerCase(id) to checkNotNull(Version.parseVersion(since)) { "Invalid Unity version '$since' for '$id' in $UNITY_SINCE_VERSIONS" }
            }
            unitySinceVersionsRef = SoftReference(versions)
        }
        return versions
    }

    private fun removeRedeclaredIds(descriptors: CssDescriptorsHolder, ussSchema: URL) {
        for (element in JDOMUtil.load(ussSchema).children) {
            // The id is in lower case, as in CssDescriptorsHolder
            val id = StringUtil.toLowerCase(element.getAttributeValue("id") ?: continue)
            when (element.name) {
                "property" -> descriptors.properties.remove(id)
                "named-value" -> descriptors.namedValues.remove(id)
                "function" -> descriptors.functions.remove(id)
                "pseudo-class" -> descriptors.pseudoSelectors.remove(id)
            }
        }
    }

    // CssDescriptorsHolder#validIdentifiers is package private, so we have to calculate it ourselves
    fun getValueIdentifiers(): Set<String> {
        var identifiers = dereference(valueIdentifiersRef)
        if (identifiers == null) {
            identifiers = mutableSetOf()
            getDescriptors().properties.values().forEach {
                it.valueDescriptor.accept(CssValueNameCollector(identifiers))
            }
            valueIdentifiersRef = SoftReference(identifiers)
        }
        return identifiers
    }

    private class CssValueNameCollector(val identifiers: MutableSet<String>) : CssValueDescriptorVisitorImpl() {
        override fun visitGroupValue(groupValue: CssGroupValue) {
            groupValue.children.forEach {
                it.accept(this)
            }
        }

        override fun visitNameValue(nameValue: CssNameValue) {
            nameValue.value?.let { identifiers.add(it) }
        }

        override fun visitTextValue(textValue: CssTextValue) {
            identifiers.add(textValue.value)
        }
    }
}