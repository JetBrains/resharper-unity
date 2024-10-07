package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.asmdef

import com.intellij.psi.PsiFile
import com.jetbrains.rider.editors.RiderCustomBackendLanguageSupport
import com.jetbrains.rider.plugins.unity.isUnityProject

class AsmDefBackendLanguageSupport : RiderCustomBackendLanguageSupport {
    override fun isAvailable(file: PsiFile): Boolean {
        if (!file.project.isUnityProject()) return false
        val extension = file.virtualFile.extension
        return extension.equals("csproj") || extension.equals("sln")
    }
}