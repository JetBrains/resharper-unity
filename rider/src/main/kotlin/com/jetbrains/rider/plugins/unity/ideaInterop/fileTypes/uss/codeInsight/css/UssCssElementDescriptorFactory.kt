package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.uss.codeInsight.css

import com.intellij.openapi.components.ServiceManager
import com.intellij.openapi.progress.ProgressManager
import com.intellij.psi.css.impl.descriptor.value.CssGroupValue
import com.intellij.psi.css.impl.descriptor.value.CssNameValue
import com.intellij.psi.css.impl.descriptor.value.CssTextValue
import com.intellij.psi.css.impl.descriptor.value.CssValueDescriptorVisitorImpl
import com.intellij.psi.css.impl.util.scheme.CssDescriptorsHolder
import com.intellij.psi.css.impl.util.scheme.CssDescriptorsLoader
import com.intellij.reference.SoftReference
import java.lang.ref.Reference

class UssCssElementDescriptorFactory {
    companion object {
        fun getInstance(): UssCssElementDescriptorFactory = ServiceManager.getService(UssCssElementDescriptorFactory::class.java)
    }

    private var cssDescriptorsHolderRef: Reference<CssDescriptorsHolder>? = null
    private var valueIdentifiersRef: Reference<Set<String>>? = null

    fun getDescriptors(): CssDescriptorsHolder {
        var descriptors = SoftReference.dereference(cssDescriptorsHolderRef)
        if (descriptors == null) {
            val progressManager = ProgressManager.getInstance()
            val loader = CssDescriptorsLoader(progressManager.progressIndicator)
            loader.loadDescriptors(this::class.java.getResource("/uss/element-descriptors.xml"))
            descriptors = loader.descriptors
            cssDescriptorsHolderRef = SoftReference(descriptors)
        }
        return descriptors
    }

    // CssDescriptorsHolder#validIdentifiers is package private, so we have to calculate it ourselves
    fun getValueIdentifiers(): Set<String> {
        var identifiers = SoftReference.dereference(valueIdentifiersRef)
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