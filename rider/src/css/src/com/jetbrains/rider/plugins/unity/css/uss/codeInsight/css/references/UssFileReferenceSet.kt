package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references

import com.intellij.openapi.fileTypes.FileType
import com.intellij.openapi.util.Condition
import com.intellij.openapi.util.SystemInfo
import com.intellij.openapi.util.TextRange
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFileSystemItem
import com.intellij.psi.css.StylesheetFile
import com.intellij.psi.css.resolve.StylesheetFileReferenceSet
import com.intellij.psi.impl.source.resolve.reference.impl.providers.FileReference
import com.intellij.psi.impl.source.resolve.reference.impl.providers.FileReferenceSet
import com.jetbrains.rider.plugins.unity.workspace.getPackages
import com.jetbrains.rider.projectDir


class UssFileReferenceSet(element: PsiElement,
                          referenceText: String,
                          textRange: TextRange,
                          private val isFont: Boolean,
                          vararg suitableFileTypes: FileType?)
    : FileReferenceSet(referenceText, element, textRange.startOffset, null, SystemInfo.isFileSystemCaseSensitive,
                       false,
                       suitableFileTypes) {

    class UssFileTypeCompletionFilter(private val myElement: PsiElement,
                                      private val isFont: Boolean,
                                      private val fileTypes: Array<FileType>) : Condition<PsiFileSystemItem> {
        // Loop-invariant: the filter is built once per completion, `value` runs once per candidate, and
        // `projectDir` resolves through the VFS on every access. `project` is read eagerly because `myElement`
        // is known valid here but not later.
        //
        // PUBLICATION rather than `lazy`'s synchronized default: the candidate loop is single-threaded, so the
        // mode is not load-bearing either way, but the default parks on a non-interruptible monitor and completion
        // runs under a read lock. `findFileByIoFile` returns canonical instances, so a duplicated initializer
        // publishes the same reference.
        private val project = myElement.project
        private val projectDir by lazy(LazyThreadSafetyMode.PUBLICATION) { project.projectDir }
        private val packagesDir by lazy(LazyThreadSafetyMode.PUBLICATION) { projectDir.findChild("Packages") }

        override fun value(item: PsiFileSystemItem?): Boolean {
            if (item == null) return false

            if (item.parent?.virtualFile == packagesDir) {
                val allPackages = WorkspaceModel.getInstance(item.project).getPackages()
                return allPackages.map { it.packageId }.contains(item.name)
            }

            if (item.isDirectory()) {
                if (item.parent?.virtualFile == projectDir)
                    return item.name == "Assets" || item.name == "Packages"

                return true
            }

            if (!myElement.isValid() || item == myElement.getContainingFile().getOriginalFile()) {
                return false
            }

            if (isFont && StylesheetFileReferenceSet.FONT_COMPLETION_FILTER.value(item))
                return true

            if (fileTypes.isEmpty()) {
                return item is StylesheetFile
            }

            return fileTypes.contains(item.getVirtualFile().fileType)
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
        return UssFileTypeCompletionFilter(element, isFont, suitableFileTypes)
    }

    private var prevReferenceText: String? = null
    override fun createFileReference(range: TextRange?, index: Int, text: String?): FileReference? {

        if (index == 0) {
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
