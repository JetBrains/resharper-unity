package com.jetbrains.rider.plugins.unity.css.uss.codeInsight.css.references

import com.intellij.openapi.util.TextRange
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.psi.*
import com.intellij.psi.impl.PsiManagerImpl
import com.intellij.psi.impl.file.PsiDirectoryImpl
import com.intellij.psi.impl.source.resolve.reference.impl.providers.FileReference
import com.intellij.util.IncorrectOperationException

class PackageFolderReference(private var set: UssFileReferenceSet,
                             range: TextRange?,
                             index: Int,
                             text: String?,
                             private var packageFolder: VirtualFile?,
                             private var completionVariants: Array<String>) : FileReference(set, range, index, text ) {
    override fun isSoft(): Boolean {
        return false
    }

    // is not called at all
    //override fun resolve(): PsiFileSystemItem? {
    //    if (packageFolder != null)
    //        return PsiDirectoryImpl(PsiManagerImpl.getInstance(set.element.project) as PsiManagerImpl, packageFolder!!)
    //    return null
    //}

    override fun multiResolve(incompleteCode: Boolean): Array<ResolveResult> {
        if (packageFolder != null){
            val manager = PsiManagerImpl.getInstance(set.element.project) as PsiManagerImpl
            return arrayOf(PsiElementResolveResult(PsiDirectoryImpl(manager, packageFolder!!)))
        }
        else return arrayOf()
    }

    @Throws(IncorrectOperationException::class)
    override fun handleElementRename(newElementName: String): PsiElement {
        throw IncorrectOperationException("Renaming Package folder is not supported.")
    }

    override fun getVariants(): Array<Any> {
        return arrayOf(*completionVariants)
    }
}
