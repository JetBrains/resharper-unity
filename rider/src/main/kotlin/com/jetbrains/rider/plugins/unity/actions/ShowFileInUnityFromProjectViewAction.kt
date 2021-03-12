package com.jetbrains.rider.plugins.unity.actions

import com.intellij.ide.projectView.ProjectView
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.util.Utils
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solution
import java.io.File


class ShowFileInUnityFromProjectViewAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val file = FileEditorManager.getInstance(project).selectedEditor?.file ?: return

        showFileInUnity(project, file)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        e.presentation.isVisible = ProjectView.getInstance(project).currentProjectViewPane is UnityExplorer
        e.presentation.isEnabled = e.project.isConnectedToEditor()
        super.update(e)
    }

    companion object {
        fun showFileInUnity(
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