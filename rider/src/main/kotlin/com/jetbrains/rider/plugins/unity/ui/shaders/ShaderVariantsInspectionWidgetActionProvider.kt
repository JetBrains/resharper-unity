package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.ActionManager
import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.markup.InspectionWidgetActionProvider
import com.intellij.util.runIf

class ShaderVariantsInspectionWidgetActionProvider : InspectionWidgetActionProvider {
    override fun createAction(editor: Editor): AnAction? = runIf(ShaderVariantsUtils.isValidContext(editor)) {
        ActionManager.getInstance().getAction("ShaderVariantWidgetAction")
    }
}