package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.ide.IconProvider
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.util.Iconable
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import javax.swing.Icon

class CgIconProvider : IconProvider(), DumbAware {
    override fun getIcon(element: PsiElement, @Iconable.IconFlags flags: Int): Icon? {
        val fileElement = element as? PsiFile
        if ((fileElement != null) && fileElement.name.endsWith(".cginc", true))
            return UnityIcons.Icons.ShaderLabFile
        return null
    }
}
