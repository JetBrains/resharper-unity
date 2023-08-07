package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFileSystemItem
import com.intellij.psi.css.resolve.StylesheetFileReferenceSet
import com.jetbrains.rider.projectDir


class UssFileReferenceSet(element: PsiElement,
                          referenceText: String,
                          textRange: TextRange,
                          vararg suitableFileTypes: FileType?)
    : StylesheetFileReferenceSet(element, referenceText,
                                 textRange,
                                 true, false,
                                 *suitableFileTypes) {

    override fun isAbsolutePathReference(): Boolean {
        val path = pathString

        return super.isAbsolutePathReference() || path.startsWith("project:/")
    }

    override fun computeDefaultContexts(): Collection<PsiFileSystemItem> {
        return if (isAbsolutePathReference) toFileSystemItems(element.project.projectDir)
        else super.computeDefaultContexts()
    }
}
