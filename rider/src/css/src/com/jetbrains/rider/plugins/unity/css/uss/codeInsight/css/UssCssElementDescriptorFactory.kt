package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
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

@Service(Service.Level.APP)
internal class UssCssElementDescriptorFactory {
    companion object {
        fun getInstance(): UssCssElementDescriptorFactory = service()
    }

    private var cssDescriptorsHolderRef: Reference<CssDescriptorsHolder>? = null
    private var valueIdentifiersRef: Reference<Set<String>>? = null

    fun getDescriptors(): CssDescriptorsHolder {
        var descriptors = dereference(cssDescriptorsHolderRef)
        if (descriptors == null) {
            val loader = CssDescriptorsLoader()
            val schemesToLoad = listOf("webref/css-cascade.xml", "webref/css-transitions.xml", "webref/css-transforms.xml", "webref/css-transforms-2.xml", "webref/css-color.xml")
            schemesToLoad.forEach {
                CssElementDescriptorFactory2.getInstance().javaClass.getResource("syntax-data/$it")!!.let { url -> loader.loadDescriptors(url) }
            }
            this::class.java.getResource("/uss/element-descriptors.xml")!!.let { loader.loadDescriptors(it) }
            descriptors = loader.descriptors
            cssDescriptorsHolderRef = SoftReference(descriptors)
        }
        return descriptors
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