package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.application.EDT
import com.intellij.openapi.client.ClientAppSession
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.put
import com.jetbrains.rdclient.editors.FrontendTextControlHostListener
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetManager
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.RdShaderVariantExtension
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ShaderVariantsHost : ProjectActivity, FrontendTextControlHostListener {
    object TextControlListener : FrontendTextControlHostListener {
        override fun beforeEditorBound(lifetime: Lifetime,
                                       appSession: ClientAppSession,
                                       textControlId: TextControlId,
                                       editorModel: TextControlModel,
                                       editor: Editor) {
            val project = editor.project ?: return
            if (ShaderVariantsUtils.isValidContext(editor)) {
                val model = FrontendBackendHost.getInstance(project).model
                model.shaderVariantExtensions.put(lifetime, textControlId, RdShaderVariantExtension())
            }
        }
    }

    override suspend fun execute(project: Project) {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        val frontendBackendHost = FrontendBackendHost.getInstance(project)
        val model = frontendBackendHost.model
        withContext(Dispatchers.EDT) {
            model.backendSettings.previewShaderVariantsSupport.advise(lifetime) {
                RiderResolveContextWidgetManager.invalidateWidgets(project)
            }
        }
    }
}