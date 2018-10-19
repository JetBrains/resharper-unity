package com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.yaml

import com.intellij.ide.IconProvider
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.util.Iconable
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiFile
import com.jetbrains.rider.plugins.unity.util.UnityIcons
import javax.swing.Icon

class UnityYamlIconProvider : IconProvider(), DumbAware {
    override fun getIcon(element: PsiElement, @Iconable.IconFlags flags: Int): Icon? {

        // TODO: Do we need this? Can we just use the default icon?
        // TODO: Different icons for different files - .yaml, .unity, .mat, etc.?
        val fileElement = element as? PsiFile
        if (fileElement != null) {
            if (fileElement.name.endsWith(".unity", true))
                return UnityIcons.FileTypes.UnityScene
            if (fileElement.name.endsWith(".meta", true))
                return UnityIcons.FileTypes.Meta
        }
        return null
    }
}
