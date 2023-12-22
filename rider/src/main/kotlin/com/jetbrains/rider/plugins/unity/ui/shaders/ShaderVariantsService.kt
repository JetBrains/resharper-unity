package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.util.runIf
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetProvider
import com.jetbrains.rider.plugins.unity.FrontendBackendHost

class ShaderVariantsService : RiderResolveContextWidgetProvider {
    override fun provideWidget(disposable: Disposable,
                               project: Project,
                               textControlId: TextControlId,
                               editorModel: TextControlModel,
                               editor: Editor
    ): RiderResolveContextWidget? = runIf(ShaderVariantsUtils.isShaderVariantSupportEnabled(project)) {
        val model = FrontendBackendHost.getInstance(project).model
        model.shaderVariantExtensions[textControlId]?.let {
            ShaderVariantWidget(project, editor, it)
        }
    }

    override fun revalidateWidget(widget: RiderResolveContextWidget,
                                  disposable: Disposable,
                                  project: Project,
                                  textControlId: TextControlId,
                                  editorModel: TextControlModel,
                                  editor: Editor): RiderResolveContextWidget? =
        widget.takeIf { ShaderVariantsUtils.isShaderVariantSupportEnabled(project) && ShaderVariantsUtils.isValidContext(editor) }
}