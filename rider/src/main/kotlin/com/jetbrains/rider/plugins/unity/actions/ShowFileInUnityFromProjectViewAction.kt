package com.jetbrains.rider.plugins.unity.actions

import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.workspaceModel.ide.impl.virtualFile
import com.jetbrains.rider.isUnityProjectFolder
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.util.Utils
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.actions.ProjectViewActionBase
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.isProjectFile
import java.io.File


class ShowFileInUnityFromProjectViewAction : ProjectViewActionBase("Show in Unity", "Shows file in Unity") {

    override fun actionPerformedInternal(entity: ProjectModelEntity, project: Project) {
        val virtualFile = entity.url?.virtualFile ?: return
        execute(project, virtualFile)
    }

    override fun getItemInternal(entity: ProjectModelEntity, project: Project): ProjectModelEntity? {
        return if (entity.isProjectFile()) entity else null
    }

    override fun updatePresentation(e: AnActionEvent, entities: List<ProjectModelEntity>) {
        val project = e.project ?: return
        e.presentation.isVisible = project.isUnityProjectFolder()
        e.presentation.isEnabled = project.isConnectedToEditor()
        super.updatePresentation(e, entities)
    }

    companion object {
        fun execute(
            project: Project,
            file: VirtualFile
        ) {
            val model = project.solution.frontendBackendModel
            val value = model.unityApplicationData.valueOrNull?.unityProcessId
            if (value != null)
                Utils.AllowUnitySetForegroundWindow(value)

            model.showFileInUnity.fire(File(file.path).relativeTo(File(project.projectDir.path)).invariantSeparatorsPath)
        }
    }
}