package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

object ShaderVariantsUtils {
    fun isValidContext(editor: Editor): Boolean = editor.project?.let { project ->
        return project.isUnityProject.getCompletedOr(false) && editor.virtualFile?.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType || it === ShaderLabFileType }
    } ?: false

    fun isShaderVariantSupportEnabled(project: Project) = project.solution.frontendBackendModel.backendSettings.previewShaderVariantsSupport.valueOrDefault(false)
}