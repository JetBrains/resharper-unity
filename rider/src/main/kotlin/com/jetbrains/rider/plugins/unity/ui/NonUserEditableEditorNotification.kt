package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotificationProvider
import com.intellij.util.ui.UIUtil
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.actions.ShowFileInUnityAction
import com.jetbrains.rider.plugins.unity.getCompletedOr
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.util.isNonEditableUnityFile
import com.jetbrains.rider.projectView.solution
import java.util.function.Function
import javax.swing.JComponent

class NonUserEditableEditorNotification : EditorNotificationProvider, DumbAware {

    override fun collectNotificationData(project: Project, file: VirtualFile): Function<in FileEditor, out JComponent?>? {
        if (project.isUnityProject.getCompletedOr(false) && isNonEditableUnityFile(file)) {
            return Function {
                EditorNotificationPanel().also { panel ->
                    panel.text = UnityUIBundle.message("label.this.file.internal.to.unity.should.not.be.edited.manually")
                    if (!file.extension.equals("meta", true)) {
                        UIUtil.invokeLaterIfNeeded {
                            addShowInUnityAction(panel, file, project)
                        }
                    }
                }
            }
        }

        return null
    }

    private fun addShowInUnityAction(panel: EditorNotificationPanel, file: VirtualFile, project: Project) {
        val model = project.solution.frontendBackendModel
        val link = panel.createActionLabel(UnityUIBundle.message("action.text.show.in.unity")) {
            ShowFileInUnityAction.execute(project, file)
        }
        link.isVisible = project.isConnectedToEditor()

        // TODO: Wrong lifetime
        model.unityEditorConnected.advise(UnityProjectLifetimeService.getLifetime(project)) { link.isVisible = it }
    }
}

