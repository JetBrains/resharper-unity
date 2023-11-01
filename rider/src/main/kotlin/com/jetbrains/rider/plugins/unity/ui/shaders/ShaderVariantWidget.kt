package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType


class ShaderVariantWidget(project: Project, editor: Editor) : AbstractShaderWidget(project, editor), RiderResolveContextWidget, Disposable {
    init {
        label.icon = ShaderLabFileType.icon
    }

    override fun showPopup(showAt: RelativePoint) = ShaderVariantPopup.show(project, showAt)
}