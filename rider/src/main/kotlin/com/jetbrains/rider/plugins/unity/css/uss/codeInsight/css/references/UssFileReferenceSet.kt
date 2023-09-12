package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.util.Condition
import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFileSystemItem
import com.intellij.psi.css.StylesheetFile
import com.intellij.psi.css.resolve.StylesheetFileReferenceSet
import com.jetbrains.rider.projectDir


class UssFileReferenceSet(element: PsiElement,
                          referenceText: String,
                          textRange: TextRange,
                          private val isFontReference:Boolean,
                          vararg suitableFileTypes: FileType?)
    : StylesheetFileReferenceSet(element, referenceText,
                                 textRange,
                                 !isFontReference, isFontReference,
                                 *suitableFileTypes) {


    class UssFileTypeCompletionFilter(private val myElement: PsiElement, private val isFontReference: Boolean, private val fileTypes: Array<FileType>) : Condition<PsiFileSystemItem> {
        override fun value(item: PsiFileSystemItem?): Boolean {
            if (item == null) return false

            if (item.isDirectory()) {
                if (item.parent?.virtualFile == item.project.projectDir)
                    return item.name == "Assets" || item.name == "Packages"

                return true
            }

            if (!myElement.isValid() || item == myElement.getContainingFile().getOriginalFile()) {
                return false
            }

            val virtualFile = item.getVirtualFile()
            if (isFontReference)
                return virtualFile.extension == "ttf"

            if (fileTypes.isEmpty()) {
                return item is StylesheetFile
            }

            return fileTypes.contains(virtualFile.fileType)
        }
    }

    override fun isAbsolutePathReference(): Boolean {
        val path = pathString

        return super.isAbsolutePathReference() || path.startsWith("project:/")
    }

    override fun computeDefaultContexts(): Collection<PsiFileSystemItem> {
        return if (isAbsolutePathReference) toFileSystemItems(element.project.projectDir)
        else super.computeDefaultContexts()
    }

    override fun getReferenceCompletionFilter(): Condition<PsiFileSystemItem> {
        return UssFileTypeCompletionFilter(element, isFontReference, suitableFileTypes)
    }
}
