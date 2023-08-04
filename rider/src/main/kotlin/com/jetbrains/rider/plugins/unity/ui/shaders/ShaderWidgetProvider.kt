package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetProvider
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer

class ShaderWidgetProvider : RiderResolveContextWidgetProvider {
    override fun provideWidget(lifetime: Lifetime,
                               project: Project,
                               textControlId: TextControlId,
                               editorModel: TextControlModel,
                               editor: Editor): RiderResolveContextWidget? {
        if (!UnityProjectDiscoverer.getInstance(project).isUnityProject)
            return null
        return ShaderWidget(project, editor).also { EditorUtil.disposeWithEditor(editor, it) }
    }
}
