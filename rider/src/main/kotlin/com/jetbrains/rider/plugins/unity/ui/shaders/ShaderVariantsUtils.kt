package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType

object ShaderVariantsUtils {
    fun isValidContext(editor: Editor): Boolean = editor.project?.let { project ->
        return UnityProjectDiscoverer.getInstance(project).isUnityProject && editor.virtualFile?.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType || it === ShaderLabFileType }
    } ?: false

    fun isShaderVariantSupportEnabled(project: Project) = FrontendBackendHost.getInstance(project).model.backendSettings.previewShaderVariantsSupport.valueOrDefault(false)
}