package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.application.Application
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.fileTypes.FileTypeRegistry
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotifications
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.ideaInterop.fileTypes.msbuild.CsprojFileType
import com.jetbrains.rider.ideaInterop.fileTypes.sln.SolutionFileType
import com.jetbrains.rider.isUnityGeneratedProject
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.model.EditorState
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.util.Utils.Companion.AllowUnitySetForegroundWindow
import com.jetbrains.rider.plugins.unity.util.isGeneratedUnityFile
import com.jetbrains.rider.plugins.unity.util.isNonEditableUnityFile
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.solution
import java.io.File

class NonUserEditableEditorNotification : EditorNotifications.Provider<EditorNotificationPanel>(), DumbAware {

    companion object {
        val KEY = Key.create<EditorNotificationPanel>("non-user.editable.source.file.editing.notification.panel")
    }

    override fun getKey(): Key<EditorNotificationPanel> = KEY

    override fun createNotificationPanel(file: VirtualFile, fileEditor: FileEditor, project: Project): EditorNotificationPanel? {

        if (project.isUnityProject() && isNonEditableUnityFile(file)) {
            val panel = EditorNotificationPanel()
            panel.setText("This file is internal to Unity and should not be edited manually.")
            addShowInUnityAction(project.lifetime, panel, file, project)
            return panel
        }

        return null
    }

    private fun addShowInUnityAction(lifetime : Lifetime, panel: EditorNotificationPanel, file: VirtualFile, project: Project) {

        val model = project.solution.rdUnityModel

        val link = panel.createActionLabel("Show in Unity") {
            val value = model.unityProcessId.valueOrNull
            if (value != null)
                AllowUnitySetForegroundWindow(value)

            model.showFileInUnity.fire(File(file.path).relativeTo(File(project.projectDir.path)).invariantSeparatorsPath)
        }

        link.isVisible = model.editorState.valueOrDefault(EditorState.Disconnected) != EditorState.Disconnected

        model.editorState.change.advise(lifetime) {
            link.isVisible = it != EditorState.Disconnected
        }
    }
}

