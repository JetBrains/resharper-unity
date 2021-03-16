package com.jetbrains.rider.plugins.unity.actions

import com.intellij.ide.actions.RevealFileAction
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.actionSystem.CommonDataKeys
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.jetbrains.rider.isUnityProjectFolder
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.util.Utils
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solution
import java.io.File

open class ShowFileInUnityAction : DumbAwareAction() {
    override fun actionPerformed(e: AnActionEvent) {
        val project = e.project ?: return
        val file = getFile(e) ?: return

        execute(project, file)
    }

    override fun update(e: AnActionEvent) {
        val project = e.project ?: return
        // see `com.intellij.ide.actions.RevealFileAction.update`
        // Cleanup context menu on selection (IDEA-245559)
        val editor = e.getData(CommonDataKeys.EDITOR)
        e.presentation.isEnabledAndVisible = project.isUnityProjectFolder() && getFile(e) != null &&
                (!ActionPlaces.isPopupPlace(e.place) || editor == null || !editor.selectionModel.hasSelection())

        e.presentation.isEnabled = project.isConnectedToEditor()
        super.update(e)
    }

    companion object {
        private fun getFile(e: AnActionEvent): VirtualFile? {
            return RevealFileAction.findLocalFile(e.getData(CommonDataKeys.VIRTUAL_FILE))
        }

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

class ShowFileInUnityFromExplorerAction: ShowFileInUnityAction() {
    // This action should be used in Explorers (Solution/FileSystem/UnityExplorer) where we do not want to have 'open in' IDEA action group
    // See RiderRevealFileAction and RIDER-52651
}