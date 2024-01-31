package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.Disposable
import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.service
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rd.ide.model.TextControlId
import com.jetbrains.rd.ide.model.TextControlModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.client.frontendProjectSession
import com.jetbrains.rdclient.editors.FrontendTextControlHost
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetManager
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidgetProvider
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class ShaderWidgetProvider : RiderResolveContextWidgetProvider, ProjectActivity {
    override fun provideWidget(disposable: Disposable,
                               project: Project,
                               textControlId: TextControlId,
                               editorModel: TextControlModel,
                               editor: Editor): RiderResolveContextWidget? =
        if (isUnityHlslFile(project, editor)) {
            ShaderWidget(project, editor).apply {
                val model = project.solution.frontendBackendModel
                setData(model.shaderContexts[textControlId.documentId])
            }
        }
        else null

    override fun revalidateWidget(widget: RiderResolveContextWidget,
                                  disposable: Disposable,
                                  project: Project,
                                  textControlId: TextControlId,
                                  editorModel: TextControlModel,
                                  editor: Editor): RiderResolveContextWidget? =
        if (isUnityHlslFile(project, editor)) widget else null

    private fun isUnityHlslFile(project: Project, editor: Editor) =
        project.isUnityProject.value && editor.virtualFile.fileType.let { it === HlslHeaderFileType || it === HlslSourceFileType }

    override suspend fun execute(project: Project) {
        withContext(Dispatchers.EDT) {
            val lifetime = UnityProjectLifetimeService.getLifetime(project)
            project.solution.isLoaded.whenTrue(lifetime) {
                if (project.isUnityProject.value) {
                    val model = project.solution.frontendBackendModel
                    adviseModel(lifetime, model, project)
                }
            }
        }
    }

    private fun adviseModel(lifetime: Lifetime, model: FrontendBackendModel, project: Project) {
        val textControlHost = project.frontendProjectSession.appSession.service<FrontendTextControlHost>()
        model.shaderContexts.advise(lifetime) { event ->
            textControlHost.getEditorsIds(event.key).forEach { editor ->
                RiderResolveContextWidgetManager.getWidget<ShaderWidget>(editor)?.setData(event.newValueOpt)
            }
        }
    }
}
