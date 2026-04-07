package com.jetbrains.rider.plugins.unity.css.uss

import com.intellij.psi.PsiFile
import com.intellij.psi.css.impl.stubs.CssStubDefinition
import com.intellij.psi.stubs.PsiFileStub
import com.intellij.psi.tree.IFileElementType
import com.intellij.psi.tree.IStubFileElementType

internal class UssFileElementType : IStubFileElementType<PsiFileStub<PsiFile>>("USS_FILE", UssLanguage) {
    override fun getStubVersion(): Int {
        return super.getStubVersion() + CssStubDefinition.BASE_VERSION
    }

    override fun getExternalId(): String {
        return "uss.file"
    }

    companion object {
        val USS_FILE: IFileElementType = UssFileElementType()
    }
}