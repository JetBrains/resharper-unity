package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.util.Condition
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.TextRange
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFileSystemItem
import com.intellij.psi.css.StylesheetFile
import com.intellij.psi.impl.source.resolve.reference.impl.providers.FileReference
import com.intellij.psi.impl.source.resolve.reference.impl.providers.FileReferenceSet
import com.jetbrains.rider.plugins.unity.workspace.getPackages
import com.jetbrains.rider.projectDir


class UssFileReferenceSet(element: PsiElement,
                          referenceText: String,
                          textRange: TextRange,
                          private val isFontReference: Boolean,
                          vararg suitableFileTypes: FileType?)
    : FileReferenceSet(referenceText, element, textRange.startOffset, null, SystemInfo.isFileSystemCaseSensitive,
                       false,
                       suitableFileTypes) {

    class UssFileTypeCompletionFilter(private val myElement: PsiElement, private val isFontReference: Boolean, private val fileTypes: Array<FileType>) : Condition<PsiFileSystemItem> {
        override fun value(item: PsiFileSystemItem?): Boolean {
            if (item == null) return false

            if (item.parent?.virtualFile == item.project.projectDir.findChild("Packages")){
                val allPackages = WorkspaceModel.getInstance(item.project).getPackages()
                return allPackages.map{it.packageId}.contains(item.name)
            }

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

    private var prevReferenceText: String? = null
    override fun createFileReference(range: TextRange?, index: Int, text: String?): FileReference? {

        if (index == 0){
            prevReferenceText = text
        }

        if (index == 1 && prevReferenceText == "Packages") {
            val packageEntities = WorkspaceModel.getInstance(element.project).getPackages()
            val packageEntity = packageEntities.singleOrNull { it.packageId == text }
            return PackageFolderReference(this, range, index, text, packageEntity?.packageFolder,
                                          packageEntities.map { it.packageId }.toTypedArray())
        }

        return super.createFileReference(range, index, text)
    }
}
