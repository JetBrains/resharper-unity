package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.lang.documentation.DocumentationProvider
import com.intellij.psi.PsiElement
import com.intellij.psi.css.CssDescriptorOwner
import com.intellij.psi.css.descriptor.CssElementDescriptor
import com.intellij.psi.css.descriptor.CssValueOwnerDescriptor
import com.intellij.psi.css.impl.util.CssDocumentationProvider
import com.intellij.psi.css.impl.util.MdnDocumentationUtil
import com.intellij.psi.css.impl.util.table.CssDescriptorsUtil

class UssDocumentationProvider : DocumentationProvider {
    override fun generateDoc(element: PsiElement?, originalElement: PsiElement?): String? {
        if (element?.containingFile?.language is UssLanguage) {
            val docElement = findDocumentationElement(element)
            if (docElement != null) {
                return generateDoc(element.text, docElement, originalElement)
            }
        }
        return null
    }

    override fun getUrlFor(element: PsiElement?, originalElement: PsiElement?): List<String>? {
        if (element?.containingFile?.language is UssLanguage) {
            // Returning an empty list overrides the default CSS documentation list, so we get the default docs
            return emptyList()
        }
        return null
    }

    private fun findDocumentationElement(element: PsiElement) =
        CssDocumentationProvider.findDocumentationElement(element)

    private fun generateDoc(descriptorText: String?,
                            documentationElement: PsiElement,
                            context: PsiElement?): String? {
        if (descriptorText == null) return null
        if (documentationElement is CssDescriptorOwner) {
            val descriptorProviderContext = context ?: documentationElement
            val descriptors = getFilteredAndSortedDescriptors(documentationElement, descriptorProviderContext)
            if (descriptors.isEmpty()) {
                return null
            }
            val latestDescriptor = descriptors.iterator().next()
            val presentableName = latestDescriptor.presentableName
            val doc = latestDescriptor.description
            if (latestDescriptor is CssValueOwnerDescriptor) {
                val valuesDescription = latestDescriptor.valuesDescription
                val formalSyntax = latestDescriptor.formalSyntax
                if (valuesDescription != null) {
                    return MdnDocumentationUtil.buildDoc(presentableName, doc, null, formalSyntax, valuesDescription)
                }
            }
            return if (doc.isNotEmpty()) {
                MdnDocumentationUtil.buildDoc(presentableName, doc, null)
            } else latestDescriptor.getDocumentationString(documentationElement)
        }
        val descriptorProvider = CssDescriptorsUtil.findDescriptorProvider(context)
        return descriptorProvider?.generateDocForSelector(descriptorText, context)
    }

    private fun getFilteredAndSortedDescriptors(descriptorOwner: CssDescriptorOwner,
                                                descriptorProviderContext: PsiElement): Collection<CssElementDescriptor> {
        var descriptors: MutableCollection<CssElementDescriptor> = descriptorOwner.getDescriptors(descriptorProviderContext).toMutableList()
        val filteredByContext = CssDescriptorsUtil.filterDescriptorsByContext(descriptors, descriptorProviderContext)
        if (!filteredByContext.isEmpty()) {
            descriptors = filteredByContext
        }
        return CssDescriptorsUtil.sortDescriptors(descriptors)
    }
}