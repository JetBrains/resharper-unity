package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetProvider
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution

class ShaderVariantsService : RiderResolveContextWidgetProvider {
    override fun provideWidget(disposable: Disposable,
                               project: Project,
                               textControlId: TextControlId,
                               editorModel: TextControlModel,
                               editor: Editor
    ): RiderResolveContextWidget? = project.solution.frontendBackendModel.shaderVariantExtensions[textControlId]?.let {
        ShaderVariantWidget(project, editor, it)
    }

    override fun revalidateWidget(widget: RiderResolveContextWidget,
                                  disposable: Disposable,
                                  project: Project,
                                  textControlId: TextControlId,
                                  editorModel: TextControlModel,
                                  editor: Editor): RiderResolveContextWidget? =
        widget.takeIf { ShaderVariantsUtils.isValidContext(editor) }
}