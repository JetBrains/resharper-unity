package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.ui.awt.RelativePoint
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel


class ShaderVariantsWidget(private val frontendBackendModel: FrontendBackendModel, project: Project, editor: Editor) : AbstractShaderWidget(project, editor), RiderResolveContextWidget, Disposable {
    init {
        label.icon = ShaderLabFileType.icon
    }

    override fun showPopup(showAt: RelativePoint) {
        val shaderVariantsSelector = ShaderVariantsSelector(frontendBackendModel)
        val popup = JBPopupFactory.getInstance().createComponentPopupBuilder(shaderVariantsSelector, shaderVariantsSelector.variants)
            .setRequestFocus(true)
            .createPopup()
        popup.show(showAt)
    }
}