package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import com.jetbrains.rider.plugins.unity.isUnityProject

object ShaderVariantsUtils {
    fun isValidContext(editor: Editor): Boolean = editor.project?.let { project ->
        return project.isUnityProject.value && editor.virtualFile?.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType || it === ShaderLabFileType }
    } ?: false
}