package com.jetbrains.rider.plugins.unity.css.uss.impl.util

import com.intellij.lang.injection.InjectedLanguageManager
import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.util.TextRange
import com.intellij.psi.*
import com.intellij.psi.css.CssDeclaration
import com.intellij.psi.css.CssTerm
import com.intellij.psi.css.CssUri
import com.intellij.psi.css.impl.util.CssReferenceProvider
import com.intellij.psi.filters.ElementFilter
import com.intellij.psi.util.CachedValueProvider
import com.intellij.psi.util.CachedValuesManager
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.util.ProcessingContext
import com.jetbrains.rider.plugins.unity.css.uss.UssLanguage
import com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references.UssFilePrefixReference
import com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references.UssFileReferenceSet


class UssReferenceProvider : PsiReferenceProvider() {
    override fun getReferencesByElement(element: PsiElement, context: ProcessingContext): Array<PsiReference> {
        return CachedValuesManager.getCachedValue(element
        ) { CachedValueProvider.Result.create(getReferences(element), element) }
    }

    class UssReferenceFilter : ElementFilter {
        override fun isAcceptable(element: Any, context: PsiElement?): Boolean {
            val psiElement = element as PsiElement
            if (!psiElement.isValid) {
                return false
            }
            if (element.containingFile.language != UssLanguage) // avoid affecting regular css files
                return false
            else if (isUriElement(psiElement))
                return true

            return false
        }

        override fun isClassAcceptable(hintClass: Class<*>?): Boolean {
            return true
        }
    }

    companion object {
        private fun getReferences(element: PsiElement): Array<PsiReference> {
            if (isUriElement(element)) {
                val referenceData = getFileReferenceData(element)
                if (referenceData != null) {
                    val isFont = isFont(element)
                    val fileTypes = if (isFont) arrayOf<FileType>() else CssReferenceProvider.IMAGE_FILE_TYPES
                    val referenceSet = UssFileReferenceSet(
                        element, referenceData.second, referenceData.third, isFont, *fileTypes)
                    val list = mutableListOf<PsiReference>(*referenceSet.allReferences)
                    list.addAll(referenceData.first)
                    return list.toTypedArray()
                }
                return arrayOf()
            }
            return arrayOf()
        }

        private fun isFont(element: PsiElement): Boolean {
            val name = PsiTreeUtil.getParentOfType(element, CssDeclaration::class.java)?.name
            return  name == "-unity-font" || name == "-unity-font-definition"
        }

        private fun getFileReferenceData(element: PsiElement?): Triple<MutableList<UssFilePrefixReference>, String, TextRange>? {
            if (element == null || !element.isValid) {
                return null
            }
            val parent = element.parent
            if (parent is PsiLanguageInjectionHost && InjectedLanguageManager.getInstance(element.project).getInjectedPsiFiles(parent) != null) {
                return null
            }
            val range = ElementManipulators.getValueTextRange(element)
            var startOffset = range.startOffset
            val endOffset = range.endOffset
            val elementText = element.text

            // trim "project:" or "project://" at the start
            // provide fake references to avoid unresolved ranges - (unresolved by CssReferenceProvider)
            val prefixReferences = mutableListOf<UssFilePrefixReference>()
            // fully ignore generated paths like
            // project://database/Assets/UI%20Images/home.quit.png?fileID=2800000&guid=eb66e14c26629fd4fb9653b5317f6dee&type=3#home.quit
            // todo: resolve to file by its guid, requires going to backend `MetaFileGuidCache`
            if (elementText.startsWith("project://database/", startOffset)) {
                prefixReferences.add(UssFilePrefixReference(element, range))
            }
            else if (elementText.startsWith("project:///", startOffset)) {
                prefixReferences.add(UssFilePrefixReference(element, TextRange.create(startOffset, startOffset + 10)))
                startOffset += 10
            }
            else if (elementText.startsWith("project:/", startOffset)) {
                prefixReferences.add(UssFilePrefixReference(element, TextRange.create(startOffset, startOffset + 8)))
                startOffset += 8
            }
            // https://github.com/Unity-Technologies/UnityCsReference/blob/b88328cf5ba7e720c9a84ac2a52e2dd237260077/ModuleOverrides/com.unity.ui/Editor/GameObjects/PanelSettingsCreator/PanelSettingsCreator.cs#L34
            else if (elementText.subSequence(startOffset, endOffset) == "unity-theme://default"){
                prefixReferences.add(UssFilePrefixReference(element, range))
            }

            val resultRange = TextRange.create(startOffset, endOffset)
            val resultText = resultRange.substring(elementText)
            return Triple(prefixReferences, resultText, resultRange)
        }

        private fun isUriElement(element: PsiElement): Boolean {
            val parent = element.parent
            return parent is CssUri || parent is CssTerm && parent.getParent() is CssUri
        }
    }
}
