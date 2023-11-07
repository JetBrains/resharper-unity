package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.util.registry.Registry
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType

object ShaderVariantsUtils {
    fun isValidContext(editor: Editor): Boolean {
        val project = editor.project ?: return false
        return Registry.`is`("rider.unity.ui.shaderVariants.enabled")
               && UnityProjectDiscoverer.getInstance(project).isUnityProject
               && editor.virtualFile.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType || it === ShaderLabFileType }
    }
}