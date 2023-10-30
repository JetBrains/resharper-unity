package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.registry.Registry
import com.intellij.util.runIf
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetProvider
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType

class ShaderVariantsService : RiderResolveContextWidgetProvider {
    override fun provideWidget(disposable: Disposable,
                               project: Project,
                               textControlId: TextControlId,
                               editorModel: TextControlModel,
                               editor: Editor): RiderResolveContextWidget? =
        runIf(isValidContext(project, editor)) {
            ShaderVariantsWidget(FrontendBackendHost.getInstance(project).model, project, editor)
        }

    override fun revalidateWidget(widget: RiderResolveContextWidget,
                                  disposable: Disposable,
                                  project: Project,
                                  textControlId: TextControlId,
                                  editorModel: TextControlModel,
                                  editor: Editor): RiderResolveContextWidget? =
        widget.takeIf { isValidContext(project, editor) }

    private fun isValidContext(project: Project, editor: Editor) =
        Registry.`is`("rider.unity.ui.shaderVariants.enabled")
        && UnityProjectDiscoverer.getInstance(project).isUnityProject
        && editor.virtualFile.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType || it === ShaderLabFileType }
}