package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveResult
import com.intellij.psi.impl.source.resolve.reference.impl.providers.PsiFileReference

class UssFilePrefixReference(private val el: PsiElement, val range: TextRange) : PsiFileReference {
    override fun getRangeInElement() = range
    override fun handleElementRename(newElementName: String) = element
    override fun bindToElement(element: PsiElement) = element
    override fun getElement(): PsiElement = el
    override fun resolve(): PsiElement { return element }

    override fun getCanonicalText(): String = element.text

    override fun isReferenceTo(element: PsiElement): Boolean {
        return false
    }

    override fun isSoft(): Boolean {
        return true
    }

    override fun multiResolve(incompleteCode: Boolean): Array<ResolveResult> {
        return arrayOf(object : ResolveResult{
            override fun getElement(): PsiElement {
                return el
            }

            override fun isValidResult(): Boolean {
                return true
            }
        })
    }
}