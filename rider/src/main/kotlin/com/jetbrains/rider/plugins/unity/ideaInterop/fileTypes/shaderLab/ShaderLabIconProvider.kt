package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.intellij.ide.IconProvider
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.util.Iconable
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import UnityIcons
import javax.swing.Icon

class ShaderLabIconProvider : IconProvider(), DumbAware {
    override fun getIcon(element: PsiElement, @Iconable.IconFlags flags: Int): Icon? {
        val fileElement = element as? PsiFile
        if ((fileElement != null) && fileElement.name.endsWith(".shader", true))
            return UnityIcons.FileTypes.ShaderLab
        return null
    }
}
