package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.cg

import com.intellij.ide.IconProvider
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.util.Iconable
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import icons.UnityIcons
import javax.swing.Icon

class CgIconProvider : IconProvider(), DumbAware {

    companion object {
        // Don't forget to update CgProjectFileType list on the backend
        val extensions: List<String> = arrayListOf( "glsl", "glslinc")
    }

    override fun getIcon(element: PsiElement, @Iconable.IconFlags flags: Int): Icon? {
        val fileElement = element as? PsiFile
        if ((fileElement != null) && extensions.any { fileElement.name.endsWith(".$it", true) })
            return UnityIcons.FileTypes.Cg
        return null
    }
}
