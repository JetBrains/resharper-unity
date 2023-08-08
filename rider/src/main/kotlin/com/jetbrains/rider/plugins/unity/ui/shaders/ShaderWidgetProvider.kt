package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetProvider
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer

class ShaderWidgetProvider : RiderResolveContextWidgetProvider {
    override fun provideWidget(disposable: Disposable,
                               project: Project,
                               textControlId: TextControlId,
                               editorModel: TextControlModel,
                               editor: Editor): RiderResolveContextWidget? =
        if (isUnityHlslFile(project, editor)) ShaderWidget(project, editor) else null

    override fun revalidateWidget(widget: RiderResolveContextWidget,
                                  disposable: Disposable,
                                  project: Project,
                                  textControlId: TextControlId,
                                  editorModel: TextControlModel,
                                  editor: Editor): RiderResolveContextWidget? =
        if (isUnityHlslFile(project, editor)) widget else null

    private fun isUnityHlslFile(project: Project, editor: Editor) =
        UnityProjectDiscoverer.getInstance(project).isUnityProject
        && editor.virtualFile.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType }
}
